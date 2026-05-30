using JRogue.Actors;
using JRogue.Manager.Party;
using JRogue.Stats;
using UnityEngine;

namespace JRogue.Status
{
    /// <summary>Party-wide status checks (rest gates, UI, etc.).</summary>
    public static class PartyStatusQueries
    {
        public static bool AnyLivingMemberHasNegativeStatus()
        {
            PartyManager party = PartyManager.Instance;
            if (party == null)
                return false;

            for (int i = 0; i < party.partyMembers.Count; i++)
            {
                BaseActor member = party.partyMembers[i];
                if (member == null)
                    continue;

                CharacterStats stats = member.stats;
                if (stats == null || stats.currentHP <= 0)
                    continue;

                StatusEffectController statuses = member.GetComponent<StatusEffectController>();
                if (statuses != null && statuses.HasNegativeStatus())
                    return true;
            }

            return false;
        }
    }
}
