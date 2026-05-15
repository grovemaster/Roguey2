using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace JRogue.Racial
{
    /// <summary>
    /// Tree-only Spirit Imprint definition for Barbarian (Phase 3). Nodes are edited as data, not per-node behaviours.
    /// </summary>
    [CreateAssetMenu(fileName = "SpiritImprintGraph", menuName = "JRogue/Racial/Spirit Imprint Graph")]
    public class SpiritImprintGraph : ScriptableObject
    {
        [Tooltip("Must match exactly one node with empty parentNodeId.")]
        public string rootNodeId = "imprint_root";

        public List<SpiritImprintNodeData> nodes = new List<SpiritImprintNodeData>();

        public bool TryFindNode(string nodeId, out SpiritImprintNodeData node)
        {
            node = null;
            if (string.IsNullOrEmpty(nodeId) || nodes == null) return false;
            for (var i = 0; i < nodes.Count; i++)
            {
                var n = nodes[i];
                if (n != null && n.nodeId == nodeId)
                {
                    node = n;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns a valid path [root, …] or null on failure. On failure, <paramref name="error"/> explains why.
        /// </summary>
        public List<string> ValidateAndNormalizePath(IReadOnlyList<string> chosenPath, out string error)
        {
            error = null;
            if (nodes == null || nodes.Count == 0)
            {
                error = "Graph has no nodes.";
                return null;
            }

            if (!TryBuildLookup(out var byId, out var setupError))
            {
                error = setupError;
                return null;
            }

            if (string.IsNullOrEmpty(rootNodeId) || !byId.TryGetValue(rootNodeId, out var rootNode))
            {
                error = $"Root id '{rootNodeId}' is missing from graph.";
                return null;
            }

            if (!string.IsNullOrEmpty(rootNode.parentNodeId))
            {
                error = "Root node must have empty parentNodeId.";
                return null;
            }

            if (rootNode.HasGameplayPayload())
            {
                error = "Root node must not carry stat, resistance, passive, or active payloads (D2.0).";
                return null;
            }

            var path = chosenPath == null || chosenPath.Count == 0
                ? new List<string> { rootNodeId }
                : new List<string>(chosenPath);

            if (path[0] != rootNodeId)
            {
                error = $"Path must start with root '{rootNodeId}'.";
                return null;
            }

            for (var i = 1; i < path.Count; i++)
            {
                if (string.IsNullOrEmpty(path[i]))
                {
                    error = "Path contains empty node id.";
                    return null;
                }

                if (!byId.TryGetValue(path[i], out var step))
                {
                    error = $"Unknown node id '{path[i]}'.";
                    return null;
                }

                if (step.parentNodeId != path[i - 1])
                {
                    error = $"Node '{path[i]}' is not a child of '{path[i - 1]}' on the path.";
                    return null;
                }
            }

            if (!ValidateSiblingExclusivity(path, byId, out var exclError))
            {
                error = exclError;
                return null;
            }

            return path;
        }

        bool TryBuildLookup(out Dictionary<string, SpiritImprintNodeData> byId, out string error)
        {
            byId = new Dictionary<string, SpiritImprintNodeData>(StringComparer.Ordinal);
            error = null;
            foreach (var n in nodes)
            {
                if (n == null || string.IsNullOrEmpty(n.nodeId))
                {
                    error = "Graph contains null node or empty nodeId.";
                    return false;
                }

                if (byId.ContainsKey(n.nodeId))
                {
                    error = $"Duplicate node id '{n.nodeId}'.";
                    return false;
                }

                byId[n.nodeId] = n;
            }

            foreach (var n in byId.Values)
            {
                if (string.IsNullOrEmpty(n.parentNodeId)) continue;
                if (!byId.ContainsKey(n.parentNodeId))
                {
                    error = $"Node '{n.nodeId}' references missing parent '{n.parentNodeId}'.";
                    return false;
                }
            }

            var rootCount = 0;
            foreach (var n in byId.Values)
            {
                if (string.IsNullOrEmpty(n.parentNodeId))
                    rootCount++;
            }

            if (rootCount != 1)
            {
                error = $"Graph must have exactly one root (empty parent); found {rootCount}.";
                return false;
            }

            if (!string.IsNullOrEmpty(byId[rootNodeId].parentNodeId))
            {
                error = "Configured rootNodeId must point to the node with empty parent.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Under a shared parent, at most one child per sibling exclusivity group may appear on the path.
        /// </summary>
        static bool ValidateSiblingExclusivity(
            IReadOnlyList<string> path,
            Dictionary<string, SpiritImprintNodeData> byId,
            out string error)
        {
            error = null;
            var used = new Dictionary<(string parentId, int group), string>();
            foreach (var id in path)
            {
                var n = byId[id];
                if (string.IsNullOrEmpty(n.parentNodeId)) continue;
                var g = n.siblingExclusivityGroup;
                if (g == 0) continue;
                var key = (n.parentNodeId, g);
                if (used.TryGetValue(key, out var other))
                {
                    var sb = new StringBuilder();
                    sb.Append("Sibling exclusivity violated: nodes '").Append(other).Append("' and '").Append(id)
                        .Append("' share parent '").Append(n.parentNodeId).Append("' and group ").Append(g).Append('.');
                    error = sb.ToString();
                    return false;
                }

                used[key] = id;
            }

            return true;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (nodes == null) return;
            TryBuildLookup(out _, out _);
        }
#endif
    }
}
