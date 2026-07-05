using System;
using UnityEngine;

namespace JRogue.Organizations
{
    [Serializable]
    public struct OrganizationMembershipRecord
    {
        public string organizationId;
        public int rank;
        public bool isActiveMember;
    }

    /// <summary>
    /// Per-actor membership and rank in world organizations (guild, temple, etc.).
    /// Not stored on <see cref="JRogue.Stats.CharacterStats"/> or player controllers.
    /// </summary>
    public sealed class OrganizationMembershipRuntime : MonoBehaviour
    {
        [SerializeField] OrganizationMembershipRecord[] memberships = Array.Empty<OrganizationMembershipRecord>();

        public bool IsMember(string organizationId)
        {
            if (!TryFindIndex(NormalizeId(organizationId), out int index))
                return false;

            return memberships[index].isActiveMember;
        }

        public bool TryGetRank(string organizationId, out int rank)
        {
            rank = 0;
            if (!TryFindIndex(NormalizeId(organizationId), out int index) || !memberships[index].isActiveMember)
                return false;

            rank = memberships[index].rank;
            return true;
        }

        public void EnsureMembership(OrganizationDefinition organization, int? startingRank = null)
        {
            if (organization == null)
                return;

            string orgId = organization.NormalizedOrganizationId;
            if (string.IsNullOrEmpty(orgId))
                return;

            int rank = startingRank ?? organization.defaultStartingRank;
            rank = organization.ClampRank(rank);

            if (TryFindIndex(orgId, out int index))
            {
                OrganizationMembershipRecord record = memberships[index];
                record.organizationId = orgId;
                record.isActiveMember = true;
                if (record.rank <= 0)
                    record.rank = rank;
                else
                    record.rank = organization.ClampRank(record.rank);
                memberships[index] = record;
                return;
            }

            AppendRecord(new OrganizationMembershipRecord
            {
                organizationId = orgId,
                rank = rank,
                isActiveMember = true,
            });
        }

        public bool TrySetRank(string organizationId, int rank)
        {
            if (!TryFindIndex(NormalizeId(organizationId), out int index) || !memberships[index].isActiveMember)
                return false;

            OrganizationMembershipRecord record = memberships[index];
            record.rank = rank;
            memberships[index] = record;
            return true;
        }

        public static OrganizationMembershipRuntime EnsureOn(GameObject actor)
        {
            if (actor == null)
                return null;

            OrganizationMembershipRuntime runtime = actor.GetComponent<OrganizationMembershipRuntime>();
            if (runtime == null)
                runtime = actor.AddComponent<OrganizationMembershipRuntime>();

            return runtime;
        }

        public static void EnsureDefaultGuildMembership(GameObject actor)
        {
            OrganizationDefinition guild = OrganizationDefinition.LoadAdventurersGuild();
            if (guild == null)
                return;

            EnsureOn(actor)?.EnsureMembership(guild);
        }

        static string NormalizeId(string organizationId) =>
            string.IsNullOrWhiteSpace(organizationId) ? string.Empty : organizationId.Trim();

        bool TryFindIndex(string organizationId, out int index)
        {
            index = -1;
            if (string.IsNullOrEmpty(organizationId) || memberships == null)
                return false;

            for (int i = 0; i < memberships.Length; i++)
            {
                if (string.Equals(NormalizeId(memberships[i].organizationId), organizationId, StringComparison.Ordinal))
                {
                    index = i;
                    return true;
                }
            }

            return false;
        }

        void AppendRecord(OrganizationMembershipRecord record)
        {
            int length = memberships?.Length ?? 0;
            var next = new OrganizationMembershipRecord[length + 1];
            if (memberships != null && length > 0)
                Array.Copy(memberships, next, length);
            next[length] = record;
            memberships = next;
        }
    }
}
