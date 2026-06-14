using System;
using System.Collections.Generic;
using JRogue.Stats.Racial;
using UnityEngine;

namespace JRogue.Racial
{
    [CreateAssetMenu(fileName = "HumanClassSkillTree", menuName = "JRogue/Racial/Human Class Skill Tree")]
    public class HumanClassSkillTreeDefinition : ScriptableObject
    {
        public HumanClass humanClass;
        public List<HumanClassSkillTreeNodeData> nodes = new List<HumanClassSkillTreeNodeData>();

        public bool TryFindNode(string nodeId, out HumanClassSkillTreeNodeData node)
        {
            node = null;
            if (string.IsNullOrEmpty(nodeId) || nodes == null)
                return false;

            for (int i = 0; i < nodes.Count; i++)
            {
                HumanClassSkillTreeNodeData n = nodes[i];
                if (n != null && n.nodeId == nodeId)
                {
                    node = n;
                    return true;
                }
            }

            return false;
        }

        public int GetTotalSpentPoints(IReadOnlyDictionary<string, int> ranksByNodeId)
        {
            if (ranksByNodeId == null)
                return 0;

            int total = 0;
            foreach (KeyValuePair<string, int> kv in ranksByNodeId)
            {
                if (kv.Value > 0)
                    total += kv.Value;
            }

            return total;
        }

        public int GetUnspentPoints(int skillPointsTotal, IReadOnlyDictionary<string, int> ranksByNodeId) =>
            Mathf.Max(0, skillPointsTotal - GetTotalSpentPoints(ranksByNodeId));

        public bool ValidateSpendPoint(
            string nodeId,
            int skillPointsTotal,
            int characterLevel,
            IReadOnlyDictionary<string, int> ranksByNodeId,
            out string failureReason)
        {
            failureReason = null;
            if (!TryFindNode(nodeId, out HumanClassSkillTreeNodeData node))
            {
                failureReason = $"Unknown node '{nodeId}'.";
                return false;
            }

            int currentRank = 0;
            if (ranksByNodeId != null)
                ranksByNodeId.TryGetValue(nodeId, out currentRank);

            if (currentRank >= node.maxRanks)
            {
                failureReason = $"Node '{nodeId}' is at max rank ({node.maxRanks}).";
                return false;
            }

            if (characterLevel < node.requiredCharacterLevel)
            {
                failureReason =
                    $"Requires character level {node.requiredCharacterLevel} (current {characterLevel}).";
                return false;
            }

            if (GetUnspentPoints(skillPointsTotal, ranksByNodeId) < 1)
            {
                failureReason = "No unspent skill points.";
                return false;
            }

            return ValidateParentPrerequisite(node, ranksByNodeId, out failureReason);
        }

        public bool TrySpendPoint(
            string nodeId,
            int skillPointsTotal,
            int characterLevel,
            Dictionary<string, int> ranksByNodeId,
            out string failureReason)
        {
            failureReason = null;
            if (!TryFindNode(nodeId, out HumanClassSkillTreeNodeData node))
            {
                failureReason = $"Unknown node '{nodeId}'.";
                return false;
            }

            ranksByNodeId ??= new Dictionary<string, int>(StringComparer.Ordinal);
            ranksByNodeId.TryGetValue(nodeId, out int currentRank);

            if (currentRank >= node.maxRanks)
            {
                failureReason = $"Node '{nodeId}' is at max rank ({node.maxRanks}).";
                return false;
            }

            if (characterLevel < node.requiredCharacterLevel)
            {
                failureReason =
                    $"Requires character level {node.requiredCharacterLevel} (current {characterLevel}).";
                return false;
            }

            if (GetUnspentPoints(skillPointsTotal, ranksByNodeId) < 1)
            {
                failureReason = "No unspent skill points.";
                return false;
            }

            if (!ValidateParentPrerequisite(node, ranksByNodeId, out failureReason))
                return false;

            ranksByNodeId[nodeId] = currentRank + 1;
            return true;
        }

        public bool TryIncrementRankFromCombat(
            string nodeId,
            int characterLevel,
            Dictionary<string, int> ranksByNodeId,
            out string failureReason)
        {
            failureReason = null;
            if (!TryFindNode(nodeId, out HumanClassSkillTreeNodeData node))
            {
                failureReason = $"Unknown node '{nodeId}'.";
                return false;
            }

            if (!node.HasActiveAbilities)
            {
                failureReason = $"Node '{nodeId}' has no actives; combat rank-up not allowed.";
                return false;
            }

            ranksByNodeId ??= new Dictionary<string, int>(StringComparer.Ordinal);
            ranksByNodeId.TryGetValue(nodeId, out int currentRank);

            if (currentRank < 1)
            {
                failureReason = $"Node '{nodeId}' is not unlocked.";
                return false;
            }

            if (currentRank >= node.maxRanks)
            {
                failureReason = $"Node '{nodeId}' is at max rank ({node.maxRanks}).";
                return false;
            }

            if (characterLevel < node.requiredCharacterLevel)
            {
                failureReason =
                    $"Requires character level {node.requiredCharacterLevel} (current {characterLevel}).";
                return false;
            }

            if (!ValidateParentPrerequisite(node, ranksByNodeId, out failureReason))
                return false;

            ranksByNodeId[nodeId] = currentRank + 1;
            return true;
        }

        static bool ValidateParentPrerequisite(
            HumanClassSkillTreeNodeData node,
            IReadOnlyDictionary<string, int> ranksByNodeId,
            out string failureReason)
        {
            failureReason = null;
            if (string.IsNullOrEmpty(node.requiredParentNodeId))
                return true;

            if (!ranksByNodeId.TryGetValue(node.requiredParentNodeId, out int parentRank) ||
                parentRank < node.requiredParentMinRank)
            {
                failureReason =
                    $"Requires '{node.requiredParentNodeId}' at rank {node.requiredParentMinRank} or higher.";
                return false;
            }

            return true;
        }
    }
}
