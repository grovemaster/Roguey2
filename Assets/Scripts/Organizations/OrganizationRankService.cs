using System;
using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Manager.Party;
using UnityEngine;

namespace JRogue.Organizations
{
    public static class OrganizationRankService
    {
        public static event Action<string, BaseActor, int, int> RankChanged;

        public static bool TryGetRank(BaseActor actor, OrganizationDefinition organization, out int rank)
        {
            rank = 0;
            if (actor == null || organization == null)
                return false;

            OrganizationMembershipRuntime membership = actor.GetComponent<OrganizationMembershipRuntime>();
            return membership != null
                && membership.TryGetRank(organization.NormalizedOrganizationId, out rank);
        }

        public static int GetScore(OrganizationDefinition organization, BaseActor actor)
        {
            if (organization == null || actor == null)
                return 0;

            return OrganizationRankScoreService.GetScore(organization.NormalizedOrganizationId, actor);
        }

        public static bool CanRankUp(
            OrganizationDefinition organization,
            BaseActor actor,
            out int targetRank,
            out string denyReason)
        {
            targetRank = 0;
            denyReason = null;

            if (organization == null || actor == null)
            {
                denyReason = "Missing organization or actor.";
                return false;
            }

            OrganizationMembershipRuntime membership = actor.GetComponent<OrganizationMembershipRuntime>();
            if (membership == null || !membership.IsMember(organization.NormalizedOrganizationId))
            {
                denyReason = "Not a member.";
                return false;
            }

            if (!membership.TryGetRank(organization.NormalizedOrganizationId, out int currentRank))
            {
                denyReason = "Not a member.";
                return false;
            }

            int essencePoints = GetScore(organization, actor);
            return OrganizationRankLogic.CanRankUp(
                organization,
                currentRank,
                essencePoints,
                out targetRank,
                out denyReason);
        }

        public static bool TryRankUp(OrganizationDefinition organization, BaseActor actor)
        {
            if (!CanRankUp(organization, actor, out int targetRank, out _))
                return false;

            OrganizationMembershipRuntime membership = actor.GetComponent<OrganizationMembershipRuntime>();
            if (membership == null || !membership.TryGetRank(organization.NormalizedOrganizationId, out int oldRank))
                return false;

            if (!membership.TrySetRank(organization.NormalizedOrganizationId, targetRank))
                return false;

            RankChanged?.Invoke(organization.NormalizedOrganizationId, actor, oldRank, targetRank);
            return true;
        }

        public static List<BaseActor> GetEligibleRankUpMembers(OrganizationDefinition organization, PartyManager party)
        {
            var eligible = new List<BaseActor>();
            if (organization == null || party?.partyMembers == null)
                return eligible;

            for (int i = 0; i < party.partyMembers.Count; i++)
            {
                BaseActor member = party.partyMembers[i];
                if (member != null && CanRankUp(organization, member, out _, out _))
                    eligible.Add(member);
            }

            return eligible;
        }

        public static int GetPartyRank(OrganizationDefinition organization, PartyManager party)
        {
            if (organization == null || party?.partyMembers == null)
                return 0;

            return OrganizationRankLogic.GetPartyRank(
                party.partyMembers,
                organization.NormalizedOrganizationId);
        }
    }
}
