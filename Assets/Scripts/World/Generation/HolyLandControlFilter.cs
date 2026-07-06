using JRogue.Actors;
using JRogue.Manager.Party;
using JRogue.Stats;
using JRogue.World.Town;

namespace JRogue.World.Generation
{
    /// <summary>Party control filter for race-restricted Holy Land floors.</summary>
    public static class HolyLandControlFilter
    {
        public static bool IsHolyLandControlFloor(string floorId) =>
            TryGetRequiredRaceForFloor(floorId, out _);

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
            if (!TryGetRequiredRaceForFloor(floorId, out Race requiredRace))
                return true;

            return member.stats != null && member.stats.race == requiredRace;
        }

        public static bool TryEnsureActiveHolyLandMember(PartyManager party)
        {
            if (party == null)
                return false;

            BaseActor active = party.GetActiveMember();
            if (active != null && IsSelectableControlTarget(active))
                return true;

            if (!TryGetRequiredRaceForFloor(GetActiveFloorId(), out Race requiredRace))
                return true;

            return TryForceActiveRaceForAdmission(party, requiredRace);
        }

        public static bool TryForceActiveBarbarianForAdmission(PartyManager party) =>
            TryForceActiveRaceForAdmission(party, Race.Barbarian);

        public static bool TryForceActiveRaceForAdmission(PartyManager party, Race requiredRace)
        {
            if (party?.partyMembers == null)
                return false;

            PartyFloorPresenceService presence = PartyFloorPresenceService.Instance;
            BaseActor active = party.GetActiveMember();
            if (active != null
                && active.stats != null
                && active.stats.currentHP > 0
                && active.stats.race == requiredRace
                && active.gameObject.activeInHierarchy
                && (presence == null || !presence.IsParked(active)))
            {
                return true;
            }

            for (int i = 0; i < party.partyMembers.Count; i++)
            {
                BaseActor member = party.partyMembers[i];
                if (member == null || member.stats == null || member.stats.currentHP <= 0)
                    continue;

                if (member.stats.race != requiredRace)
                    continue;

                if (presence != null && presence.IsParked(member))
                    continue;

                if (!member.gameObject.activeInHierarchy)
                    continue;

                party.SwapActiveMember(i);
                return true;
            }

            return false;
        }

        public static bool TryGetRequiredRaceForFloor(string floorId, out Race race)
        {
            if (floorId == HolyLandFloorIds.HolyLandProper
                || floorId == HolyLandFloorIds.ShamanTentInterior)
            {
                race = Race.Barbarian;
                return true;
            }

            if (floorId == HolyLandFloorIds.ElfHolyLandProper
                || floorId == HolyLandFloorIds.ElfHouseInterior)
            {
                race = Race.Elf;
                return true;
            }

            if (floorId == HolyLandFloorIds.BeastmanHolyLandProper
                || floorId == HolyLandFloorIds.BeastmanDenInterior)
            {
                race = Race.Beastman;
                return true;
            }

            race = default;
            return false;
        }
    }
}
