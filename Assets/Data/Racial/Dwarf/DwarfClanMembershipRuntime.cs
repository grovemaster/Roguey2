using System;
using UnityEngine;

namespace JRogue.Racial
{
    /// <summary>
    /// Tracks which dwarf clan a character belongs to and personal clan rank.
    /// </summary>
    public sealed class DwarfClanMembershipRuntime : MonoBehaviour
    {
        [SerializeField] string clanId;
        [SerializeField] int clanMemberRank;

        public string ClanId => clanId;
        public int ClanMemberRank => clanMemberRank;
        public bool IsAffiliated => !string.IsNullOrEmpty(clanId);

        public static DwarfClanMembershipRuntime EnsureOn(GameObject actor)
        {
            if (actor == null)
                return null;

            DwarfClanMembershipRuntime runtime = actor.GetComponent<DwarfClanMembershipRuntime>();
            if (runtime == null)
                runtime = actor.AddComponent<DwarfClanMembershipRuntime>();

            return runtime;
        }

        public void SetMembership(string newClanId, int rank)
        {
            clanId = string.IsNullOrWhiteSpace(newClanId) ? string.Empty : newClanId.Trim();
            clanMemberRank = Mathf.Max(0, rank);
        }

        public void IncrementMemberRank() => clanMemberRank = Mathf.Max(0, clanMemberRank + 1);

        public bool MatchesClan(DwarfClanDefinition clan) =>
            clan != null
            && !string.IsNullOrEmpty(clan.clanId)
            && string.Equals(clanId, clan.clanId.Trim(), StringComparison.Ordinal);
    }
}
