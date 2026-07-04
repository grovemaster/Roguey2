using JRogue.Actors;
using JRogue.Manager.Party;
using JRogue.Stats;
using JRogue.World.Town;

namespace JRogue.World.Generation
{
    /// <summary>Party control filter for Barbarian-only Holy Land floors.</summary>
    public static class HolyLandControlFilter
    {
        public static bool IsHolyLandControlFloor(string floorId) =>
            floorId == HolyLandFloorIds.HolyLandProper
            || floorId == HolyLandFloorIds.ShamanTentInterior;

        public static string GetActiveFloorId()
        {
            DungeonFloorInstanceManager manager = DungeonFloorInstanceManager.Instance;
            return manager?.GetActiveFloorInstance()?.Definition?.FloorId;
        }

        public static bool IsSelectableControlTarget(BaseActor member)
        {
            if (member == null)
                return false;

            PartyFloorPresenceService presence = PartyFloorPresenceService.Instance;
            if (presence != null && presence.IsParked(member))
                return false;

            string floorId = GetActiveFloorId();
            if (!IsHolyLandControlFloor(floorId))
                return true;

            return member.stats != null && member.stats.race == Race.Barbarian;
        }

        public static bool TryEnsureActiveBarbarian(PartyManager party)
        {
            if (party == null)
                return false;

            BaseActor active = party.GetActiveMember();
            if (active != null && IsSelectableControlTarget(active))
                return true;

            return TryForceActiveBarbarianForAdmission(party);
        }

        /// <summary>
        /// Holy Land admission always controls a living Barbarian, even on the nexus
        /// where non-Barbarians are normally selectable.
        /// </summary>
        public static bool TryForceActiveBarbarianForAdmission(PartyManager party)
        {
            if (party?.partyMembers == null)
                return false;

            BaseActor active = party.GetActiveMember();
            if (active != null
                && active.stats != null
                && active.stats.currentHP > 0
                && active.stats.race == Race.Barbarian
                && active.gameObject.activeInHierarchy)
            {
                return true;
            }

            for (int i = 0; i < party.partyMembers.Count; i++)
            {
                BaseActor member = party.partyMembers[i];
                if (member == null || member.stats == null || member.stats.currentHP <= 0)
                    continue;

                if (member.stats.race != Race.Barbarian)
                    continue;

                party.SwapActiveMember(i);
                return true;
            }

            return false;
        }
    }
}
