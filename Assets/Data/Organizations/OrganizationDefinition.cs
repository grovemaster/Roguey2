using UnityEngine;

namespace JRogue.Organizations
{
    [CreateAssetMenu(fileName = "Organization_", menuName = "JRogue/Organization Definition")]
    public sealed class OrganizationDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string organizationId = OrganizationIds.AdventurersGuild;
        public string displayName = "Adventurer's Guild";

        [Header("Rank scale")]
        [Tooltip("Highest standing (best rank). Guild: 1.")]
        [Min(1)] public int rankBest = 1;

        [Tooltip("Lowest standing (worst rank). Guild: 9.")]
        [Min(1)] public int rankWorst = 9;

        [Tooltip("Starting rank for new members.")]
        [Min(1)] public int defaultStartingRank = 9;

        public bool allowsRankDecrease;

        [Header("Rank thresholds")]
        [Tooltip("Essence points required to hold each rank tier. Index 0 = rankWorst, last = rankBest.")]
        public int[] rankThresholds = { 0, 3, 6, 9, 12, 15, 18, 21, 24 };

        public string NormalizedOrganizationId =>
            string.IsNullOrWhiteSpace(organizationId) ? string.Empty : organizationId.Trim();

        public int GetThresholdForRank(int rank)
        {
            int worst = Mathf.Max(rankBest, rankWorst);
            int best = Mathf.Min(rankBest, rankWorst);
            rank = Mathf.Clamp(rank, best, worst);

            if (rankThresholds == null || rankThresholds.Length == 0)
                return (worst - rank) * 3;

            int index = worst - rank;
            if (index < 0 || index >= rankThresholds.Length)
                return (worst - rank) * 3;

            return Mathf.Max(0, rankThresholds[index]);
        }

        public bool IsValidRank(int rank)
        {
            int worst = Mathf.Max(rankBest, rankWorst);
            int best = Mathf.Min(rankBest, rankWorst);
            return rank >= best && rank <= worst;
        }

        public int ClampRank(int rank)
        {
            int worst = Mathf.Max(rankBest, rankWorst);
            int best = Mathf.Min(rankBest, rankWorst);
            return Mathf.Clamp(rank, best, worst);
        }

        public static OrganizationDefinition LoadAdventurersGuild() =>
            Resources.Load<OrganizationDefinition>("Organizations/Organization_AdventurersGuild");
    }
}
