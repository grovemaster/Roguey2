using System;
using System.Collections.Generic;
using JRogue.Stats;
using JRogue.Stats.Racial;
using UnityEngine;

namespace JRogue.Racial
{
    sealed class DwarfAncestorNodeModifierSource : IEquatable<DwarfAncestorNodeModifierSource>
    {
        public AncestorDefinition Patron { get; }
        public string NodeId { get; }

        public DwarfAncestorNodeModifierSource(AncestorDefinition patron, string nodeId)
        {
            Patron = patron;
            NodeId = nodeId;
        }

        public bool Equals(DwarfAncestorNodeModifierSource other) =>
            other != null && Patron == other.Patron && NodeId == other.NodeId;

        public override bool Equals(object obj) => obj is DwarfAncestorNodeModifierSource o && Equals(o);

        public override int GetHashCode() =>
            HashCode.Combine(Patron != null ? Patron.GetEntityId().GetHashCode() : 0, NodeId);
    }

    /// <summary>
    /// Optional patron Ancestor with a forward-only preset path on <see cref="AncestorDefinition.abilityTree"/> (Pattern B).
    /// </summary>
    [DefaultExecutionOrder(52)]
    public class DwarfAncestorPathRuntime : MonoBehaviour
    {
        [SerializeField] AncestorDefinition patronAncestor;

        [Tooltip("Ordered spine root → deepest on patron's ability tree. Ignored when patron is unset.")]
        [SerializeField] List<string> chosenPathNodeIds = new List<string>();

        [SerializeField] bool requireDwarfAncestrySubsystem = true;

        readonly Dictionary<string, DwarfAncestorNodeModifierSource> _modifierSources =
            new Dictionary<string, DwarfAncestorNodeModifierSource>();

        bool _applied;

        public AncestorDefinition PatronAncestor => patronAncestor;

        public int AncestorRank =>
            patronAncestor == null || chosenPathNodeIds == null || chosenPathNodeIds.Count == 0
                ? 0
                : chosenPathNodeIds.Count - 1;

        public IReadOnlyList<string> ChosenPathNodeIds => chosenPathNodeIds;

        public bool IsNodeLearned(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId) || chosenPathNodeIds == null)
                return false;

            for (int i = 0; i < chosenPathNodeIds.Count; i++)
            {
                if (string.Equals(chosenPathNodeIds[i], nodeId, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        public bool TryAppendLearnedNode(string nodeId, out string failureReason)
        {
            failureReason = null;
            if (patronAncestor == null || patronAncestor.abilityTree == null)
            {
                failureReason = "No patron ability tree.";
                return false;
            }

            SpiritImprintGraph graph = patronAncestor.abilityTree;
            if (!graph.TryFindNode(nodeId, out SpiritImprintNodeData node))
            {
                failureReason = $"Unknown node '{nodeId}'.";
                return false;
            }

            if (IsNodeLearned(nodeId))
            {
                failureReason = $"Node '{nodeId}' is already learned.";
                return false;
            }

            if (string.IsNullOrEmpty(node.parentNodeId))
            {
                failureReason = "Root is learned on join.";
                return false;
            }

            if (!IsNodeLearned(node.parentNodeId))
            {
                failureReason = $"Parent '{node.parentNodeId}' is not learned.";
                return false;
            }

            var trial = chosenPathNodeIds == null || chosenPathNodeIds.Count == 0
                ? new List<string> { graph.rootNodeId, nodeId }
                : new List<string>(chosenPathNodeIds) { nodeId };

            List<string> normalized = graph.ValidateAndNormalizeLearnedSet(trial, out failureReason);
            if (normalized == null)
                return false;

            chosenPathNodeIds = normalized;
            TryApplyFromSerializedState();
            return true;
        }

        public void SetPatronAndPath(AncestorDefinition patron, IReadOnlyList<string> path)
        {
            patronAncestor = patron;
            chosenPathNodeIds = path == null ? new List<string>() : new List<string>(path);
        }

        void Awake()
        {
            if (chosenPathNodeIds == null)
                chosenPathNodeIds = new List<string>();
        }

        void Start() => TryApplyFromSerializedState();

        void OnDestroy() => RemoveApplied();

        public void TryApplyFromSerializedState()
        {
            if (patronAncestor == null)
            {
                RemoveApplied();
                return;
            }

            var stats = GetComponent<CharacterStats>();
            if (stats == null)
            {
                Debug.LogWarning($"[DwarfAncestor] {name} has no CharacterStats.");
                return;
            }

            if (!ValidateDwarfActor(stats, out _))
                return;

            SpiritImprintGraph graph = patronAncestor.abilityTree;
            if (graph == null)
            {
                Debug.LogWarning($"[DwarfAncestor] Patron '{patronAncestor.name}' has no abilityTree.");
                RemoveApplied();
                return;
            }

            List<string> normalized = graph.ValidateAndNormalizePath(chosenPathNodeIds, out string error);
            if (normalized == null)
            {
                Debug.LogWarning($"[DwarfAncestor] Invalid path on '{name}': {error}. Falling back to root only.");
                normalized = new List<string> { graph.rootNodeId };
            }

            if (_applied)
                RemoveApplied();

            chosenPathNodeIds = normalized;
            ApplyPath(stats, graph);
            _applied = true;
        }

        void ApplyPath(CharacterStats stats, SpiritImprintGraph graph)
        {
            foreach (string nodeId in chosenPathNodeIds)
            {
                if (!graph.TryFindNode(nodeId, out SpiritImprintNodeData node))
                    continue;

                if (!_modifierSources.TryGetValue(nodeId, out DwarfAncestorNodeModifierSource src))
                {
                    src = new DwarfAncestorNodeModifierSource(patronAncestor, nodeId);
                    _modifierSources[nodeId] = src;
                }

                RacialAbilityPayloadApplicator.ApplyNodePayload(gameObject, stats, src, node);
            }
        }

        void RemoveApplied()
        {
            if (!_applied || patronAncestor == null)
            {
                _modifierSources.Clear();
                _applied = false;
                return;
            }

            SpiritImprintGraph graph = patronAncestor.abilityTree;
            CharacterStats stats = GetComponent<CharacterStats>();

            if (graph != null && stats != null)
            {
                foreach (string nodeId in chosenPathNodeIds)
                {
                    if (!graph.TryFindNode(nodeId, out SpiritImprintNodeData node))
                        continue;
                    if (_modifierSources.TryGetValue(nodeId, out DwarfAncestorNodeModifierSource src))
                        RacialAbilityPayloadApplicator.RemoveNodePayload(gameObject, stats, src, node);
                }
            }

            _modifierSources.Clear();
            _applied = false;
        }

        public void RefreshPassives()
        {
            if (!_applied || patronAncestor?.abilityTree == null)
                return;

            SpiritImprintGraph graph = patronAncestor.abilityTree;
            foreach (string nodeId in chosenPathNodeIds)
            {
                if (graph.TryFindNode(nodeId, out SpiritImprintNodeData node))
                    RacialAbilityPayloadApplicator.RefreshPassives(gameObject, node.passiveEffects);
            }
        }

        public void NotifyPassivesTurnStart()
        {
            if (!_applied || patronAncestor?.abilityTree == null)
                return;

            SpiritImprintGraph graph = patronAncestor.abilityTree;
            foreach (string nodeId in chosenPathNodeIds)
            {
                if (graph.TryFindNode(nodeId, out SpiritImprintNodeData node))
                    RacialAbilityPayloadApplicator.NotifyPassivesTurnStart(gameObject, node.passiveEffects);
            }
        }

        bool ValidateDwarfActor(CharacterStats stats, out string failureReason)
        {
            failureReason = null;
            if (stats.race != Race.Dwarf)
            {
                failureReason = "Not a Dwarf.";
                return false;
            }

            if (requireDwarfAncestrySubsystem &&
                stats.racialSubsystem != RacialSubsystemKind.DwarfAncestry)
            {
                failureReason = "Racial subsystem is not DwarfAncestry.";
                return false;
            }

            return true;
        }
    }
}
