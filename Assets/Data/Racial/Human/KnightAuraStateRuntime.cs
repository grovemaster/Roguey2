using System;
using System.Collections.Generic;
using UnityEngine;

namespace JRogue.Racial
{
    /// <summary>
    /// Tracks which Knight aura skills are toggled on. At most one <see cref="KnightSkillTag.AuraStance"/> active.
    /// </summary>
    public sealed class KnightAuraStateRuntime : MonoBehaviour
    {
        [SerializeField] string activeStanceNodeId;
        [SerializeField] List<string> activeAuraNodeIds = new List<string>();

        readonly HashSet<string> _activeAuras = new HashSet<string>(StringComparer.Ordinal);

        void OnEnable() => RebuildIndex();

        public static KnightAuraStateRuntime EnsureOn(GameObject actor)
        {
            if (actor == null)
                return null;

            KnightAuraStateRuntime runtime = actor.GetComponent<KnightAuraStateRuntime>();
            if (runtime == null)
                runtime = actor.AddComponent<KnightAuraStateRuntime>();

            runtime.RebuildIndex();
            return runtime;
        }

        void RebuildIndex()
        {
            _activeAuras.Clear();
            if (activeAuraNodeIds == null)
                return;

            for (int i = 0; i < activeAuraNodeIds.Count; i++)
            {
                string nodeId = activeAuraNodeIds[i];
                if (!string.IsNullOrEmpty(nodeId))
                    _activeAuras.Add(nodeId);
            }
        }

        public string ActiveStanceNodeId => activeStanceNodeId;

        public bool IsActive(string nodeId) =>
            !string.IsNullOrEmpty(nodeId) && _activeAuras.Contains(nodeId);

        public bool TryActivate(
            HumanClassSkillTreeDefinition tree,
            string nodeId,
            out bool wasAlreadyActive,
            out string failureReason)
        {
            wasAlreadyActive = false;
            failureReason = null;

            if (string.IsNullOrEmpty(nodeId))
            {
                failureReason = "Node id is empty.";
                return false;
            }

            if (!tree.TryFindNode(nodeId, out HumanClassSkillTreeNodeData node))
            {
                failureReason = $"Unknown node '{nodeId}'.";
                return false;
            }

            if (IsActive(nodeId))
            {
                wasAlreadyActive = true;
                return true;
            }

            if (node.IsAuraStance)
            {
                if (!string.IsNullOrEmpty(activeStanceNodeId) && activeStanceNodeId != nodeId)
                    DeactivateInternal(activeStanceNodeId);

                activeStanceNodeId = nodeId;
            }

            AddActive(nodeId);
            return true;
        }

        public bool TryDeactivate(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId) || !IsActive(nodeId))
                return false;

            DeactivateInternal(nodeId);
            return true;
        }

        void DeactivateInternal(string nodeId)
        {
            _activeAuras.Remove(nodeId);
            activeAuraNodeIds?.RemoveAll(id => string.Equals(id, nodeId, StringComparison.Ordinal));

            if (string.Equals(activeStanceNodeId, nodeId, StringComparison.Ordinal))
                activeStanceNodeId = null;
        }

        void AddActive(string nodeId)
        {
            _activeAuras.Add(nodeId);
            activeAuraNodeIds ??= new List<string>();
            if (!activeAuraNodeIds.Contains(nodeId))
                activeAuraNodeIds.Add(nodeId);
        }
    }
}
