using System;
using System.Collections.Generic;
using UnityEngine;

namespace JRogue.Racial
{
    [CreateAssetMenu(fileName = "UndeadSkillTree", menuName = "JRogue/Racial/Undead Skill Tree")]
    public class UndeadSkillTreeDefinition : ScriptableObject
    {
        public List<UndeadSkillTreeClusterData> clusters = new List<UndeadSkillTreeClusterData>();
        public List<UndeadSkillTreeNodeData> nodes = new List<UndeadSkillTreeNodeData>();

        public bool TryFindNode(string nodeId, out UndeadSkillTreeNodeData node)
        {
            node = null;
            if (string.IsNullOrEmpty(nodeId) || nodes == null)
                return false;

            for (int i = 0; i < nodes.Count; i++)
            {
                UndeadSkillTreeNodeData n = nodes[i];
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

        public bool IsClusterUnlocked(string clusterId, IReadOnlyDictionary<string, int> ranksByNodeId)
        {
            if (clusters == null || string.IsNullOrEmpty(clusterId))
                return false;

            int threshold = 0;
            for (int i = 0; i < clusters.Count; i++)
            {
                UndeadSkillTreeClusterData c = clusters[i];
                if (c != null && c.clusterId == clusterId)
                {
                    threshold = c.pointsRequiredToUnlock;
                    break;
                }
            }

            return GetTotalSpentPoints(ranksByNodeId) >= threshold;
        }

        public bool TrySpendPoint(
            string nodeId,
            int skillPointsTotal,
            Dictionary<string, int> ranksByNodeId,
            out string failureReason)
        {
            failureReason = null;
            if (!TryFindNode(nodeId, out UndeadSkillTreeNodeData node))
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

            if (GetUnspentPoints(skillPointsTotal, ranksByNodeId) < 1)
            {
                failureReason = "No unspent skill points.";
                return false;
            }

            if (!IsClusterUnlocked(node.clusterId, ranksByNodeId))
            {
                failureReason = $"Cluster '{node.clusterId}' is locked.";
                return false;
            }

            if (!ValidateParentPrerequisite(node, ranksByNodeId, out failureReason))
                return false;

            if (!ValidateExclusivityForSpend(node, ranksByNodeId, out failureReason))
                return false;

            ranksByNodeId[nodeId] = currentRank + 1;
            return true;
        }

        public bool TryRefundRank(string nodeId, Dictionary<string, int> ranksByNodeId, out string failureReason)
        {
            failureReason = null;
            if (ranksByNodeId == null || !ranksByNodeId.TryGetValue(nodeId, out int currentRank) || currentRank < 1)
            {
                failureReason = $"Node '{nodeId}' has no rank to refund.";
                return false;
            }

            if (!TryFindNode(nodeId, out _))
            {
                failureReason = $"Unknown node '{nodeId}'.";
                return false;
            }

            if (currentRank == 1)
                ranksByNodeId.Remove(nodeId);
            else
                ranksByNodeId[nodeId] = currentRank - 1;

            return true;
        }

        bool ValidateParentPrerequisite(UndeadSkillTreeNodeData node, IReadOnlyDictionary<string, int> ranksByNodeId, out string failureReason)
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

            if (node.nodeKind == UndeadSkillNodeKind.Upgrade)
            {
                if (!TryFindNode(node.requiredParentNodeId, out UndeadSkillTreeNodeData parent))
                {
                    failureReason = $"Upgrade parent '{node.requiredParentNodeId}' is missing.";
                    return false;
                }

                if (parent.nodeKind != UndeadSkillNodeKind.Skill)
                {
                    failureReason = $"Upgrade '{node.nodeId}' must attach to a Skill parent.";
                    return false;
                }
            }

            return true;
        }

        bool ValidateExclusivityForSpend(
            UndeadSkillTreeNodeData node,
            IReadOnlyDictionary<string, int> ranksByNodeId,
            out string failureReason)
        {
            failureReason = null;
            if (node.mutualExclusivityGroup == 0 || nodes == null)
                return true;

            for (int i = 0; i < nodes.Count; i++)
            {
                UndeadSkillTreeNodeData other = nodes[i];
                if (other == null || other.nodeId == node.nodeId)
                    continue;
                if (other.mutualExclusivityGroup != node.mutualExclusivityGroup)
                    continue;

                if (ranksByNodeId.TryGetValue(other.nodeId, out int otherRank) && otherRank > 0)
                {
                    failureReason =
                        $"Exclusivity group {node.mutualExclusivityGroup}: '{other.nodeId}' is already ranked.";
                    return false;
                }
            }

            return true;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (nodes == null)
                return;

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (UndeadSkillTreeNodeData n in nodes)
            {
                if (n == null || string.IsNullOrEmpty(n.nodeId))
                    continue;
                if (!ids.Add(n.nodeId))
                    Debug.LogWarning($"[UndeadSkillTree] Duplicate node id '{n.nodeId}' on {name}.", this);
            }
        }
#endif
    }
}
