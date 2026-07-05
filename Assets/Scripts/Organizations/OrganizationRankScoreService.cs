using System;
using System.Collections.Generic;
using JRogue.Actors;

namespace JRogue.Organizations
{
    public static class OrganizationRankScoreService
    {
        static readonly Dictionary<string, List<IOrganizationRankScoreContributor>> ContributorsByOrg =
            new Dictionary<string, List<IOrganizationRankScoreContributor>>(StringComparer.Ordinal);

        static OrganizationRankScoreService()
        {
            RegisterContributor(EssenceSlotScoreContributor.Instance);
        }

        public static void RegisterContributor(IOrganizationRankScoreContributor contributor)
        {
            if (contributor == null || string.IsNullOrWhiteSpace(contributor.OrganizationId))
                return;

            string orgId = contributor.OrganizationId.Trim();
            if (!ContributorsByOrg.TryGetValue(orgId, out List<IOrganizationRankScoreContributor> list))
            {
                list = new List<IOrganizationRankScoreContributor>();
                ContributorsByOrg[orgId] = list;
            }

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].GetType() == contributor.GetType())
                    return;
            }

            list.Add(contributor);
        }

        public static int GetScore(string organizationId, BaseActor actor)
        {
            if (actor == null || string.IsNullOrWhiteSpace(organizationId))
                return 0;

            string orgId = organizationId.Trim();
            if (!ContributorsByOrg.TryGetValue(orgId, out List<IOrganizationRankScoreContributor> list))
                return 0;

            int total = 0;
            for (int i = 0; i < list.Count; i++)
                total += list[i].Contribute(actor);

            return total;
        }

        public static void ResetContributorsForTests()
        {
            ContributorsByOrg.Clear();
            RegisterContributor(EssenceSlotScoreContributor.Instance);
        }
    }
}
