using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Manager.Party;

namespace JRogue.Organizations
{
    public static class OrganizationRankLogic
    {
        public static int GetTargetRank(OrganizationDefinition organization, int currentRank)
        {
            if (organization == null)
                return currentRank;

            int best = UnityEngine.Mathf.Min(organization.rankBest, organization.rankWorst);
            return UnityEngine.Mathf.Max(best, currentRank - 1);
        }

        public static bool CanRankUp(
            OrganizationDefinition organization,
            int currentRank,
            int essencePoints,
            out int targetRank,
            out string denyReason)
        {
            targetRank = currentRank;
            denyReason = null;

            if (organization == null)
            {
                denyReason = "Unknown organization.";
                return false;
            }

            int best = UnityEngine.Mathf.Min(organization.rankBest, organization.rankWorst);
            if (currentRank <= best)
            {
                denyReason = "Already at the highest rank.";
                return false;
            }

            targetRank = GetTargetRank(organization, currentRank);
            int threshold = organization.GetThresholdForRank(targetRank);
            if (essencePoints < threshold)
            {
                denyReason = $"Requires at least {threshold} essence points.";
                return false;
            }

            return true;
        }

        public static int GetPartyRank(IReadOnlyList<BaseActor> members, string organizationId)
        {
            if (members == null || members.Count == 0 || string.IsNullOrWhiteSpace(organizationId))
                return 0;

            string orgId = organizationId.Trim();
            int count = 0;
            int sum = 0;

            for (int i = 0; i < members.Count; i++)
            {
                BaseActor member = members[i];
                if (member == null)
                    continue;

                OrganizationMembershipRuntime membership = member.GetComponent<OrganizationMembershipRuntime>();
                if (membership == null || !membership.TryGetRank(orgId, out int rank))
                    continue;

                sum += rank;
                count++;
            }

            if (count == 0)
                return 0;

            return UnityEngine.Mathf.FloorToInt(sum / (float)count);
        }
    }
}
