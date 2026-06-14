using JRogue.Actors;
using JRogue.Stats;
using UnityEngine;

namespace JRogue.Racial
{
    public static class DwarfAncestorLearnService
    {
        public static bool TryLearnNode(
            BaseActor speaker,
            DwarfClanDefinition clan,
            string nodeId,
            out string failureReason)
        {
            failureReason = null;
            if (string.IsNullOrWhiteSpace(nodeId))
            {
                failureReason = "No technique selected.";
                return false;
            }

            if (!DwarfAncestorLearnLogic.CanBeginAltarCeremony(out failureReason))
                return false;

            return ApplyLearnNode(speaker, clan, nodeId, out failureReason);
        }

        public static bool ApplyLearnNode(
            BaseActor speaker,
            DwarfClanDefinition clan,
            string nodeId,
            out string failureReason)
        {
            failureReason = null;
            if (string.IsNullOrWhiteSpace(nodeId))
            {
                failureReason = "No technique selected.";
                return false;
            }

            if (!DwarfAncestorLearnLogic.IsSpeakerEligibleForAltar(
                    speaker,
                    clan,
                    out DwarfClanMembershipRuntime membership,
                    out DwarfAncestorPathRuntime pathRuntime,
                    out failureReason))
            {
                return false;
            }

            if (!pathRuntime.PatronAncestor.abilityTree.TryFindNode(nodeId, out SpiritImprintNodeData node))
            {
                failureReason = $"Unknown node '{nodeId}'.";
                return false;
            }

            int clanPrestige = DwarfClanWorldState.EnsureInstance().GetPrestige(clan.clanId);
            if (!DwarfAncestorLearnLogic.PassesGates(
                    node,
                    speaker.GetComponent<CharacterStats>(),
                    membership.ClanMemberRank,
                    clanPrestige,
                    out failureReason))
            {
                return false;
            }

            if (!pathRuntime.TryAppendLearnedNode(nodeId, out failureReason))
                return false;

            if (nodeId != pathRuntime.PatronAncestor.abilityTree.rootNodeId)
                membership.IncrementMemberRank();

            Debug.Log(
                $"[DwarfClan] {speaker.DisplayName} learned Ancestor node '{nodeId}' at clan '{clan.clanId}'.");
            return true;
        }
    }
}
