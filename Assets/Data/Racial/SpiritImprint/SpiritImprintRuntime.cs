using System;
using System.Collections.Generic;
using JRogue.Item.Essence;
using JRogue.Stats;
using JRogue.Stats.Racial;
using UnityEngine;

namespace JRogue.Racial
{
    /// <summary>
    /// Per-node modifier source token so stats/resistances stack and remove independently from racial loadout and other nodes.
    /// </summary>
    sealed class SpiritImprintNodeModifierSource : IEquatable<SpiritImprintNodeModifierSource>
    {
        public SpiritImprintGraph Graph { get; }
        public string NodeId { get; }

        public SpiritImprintNodeModifierSource(SpiritImprintGraph graph, string nodeId)
        {
            Graph = graph;
            NodeId = nodeId;
        }

        public bool Equals(SpiritImprintNodeModifierSource other) =>
            other != null && Graph == other.Graph && NodeId == other.NodeId;

        public override bool Equals(object obj) => obj is SpiritImprintNodeModifierSource o && Equals(o);

        public override int GetHashCode() =>
            HashCode.Combine(Graph != null ? Graph.GetEntityId().GetHashCode() : 0, NodeId);
    }

    /// <summary>
    /// Applies <see cref="SpiritImprintGraph"/> payloads for the serialized <see cref="chosenPathNodeIds"/> spine (Pattern B).
    /// Phase 3 v0: path is authored on the prefab; optional dev append API is stripped from release builds.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class SpiritImprintRuntime : MonoBehaviour
    {
        [SerializeField] SpiritImprintGraph graph;

        [Tooltip("Ordered spine from root → deepest (canonical save).")]
        [SerializeField] List<string> chosenPathNodeIds = new List<string>();

        [Tooltip("If true, requires CharacterStats.racialSubsystem == SpiritImprintBarbarian.")]
        [SerializeField] bool requireSpiritImprintSubsystem = true;

        readonly Dictionary<string, SpiritImprintNodeModifierSource> _modifierSources = new Dictionary<string, SpiritImprintNodeModifierSource>();

        bool _applied;

        public SpiritImprintGraph Graph => graph;

        /// <summary>Non-root count; invariant: <see cref="ImprintRank"/> == chosenPathNodeIds.Count - 1 when path starts with root.</summary>
        public int ImprintRank => chosenPathNodeIds is { Count: > 0 } ? chosenPathNodeIds.Count - 1 : 0;

        public IReadOnlyList<string> ChosenPathNodeIds => chosenPathNodeIds;

        /// <summary>Assign graph and spine before <see cref="TryApplyFromSerializedState"/> (tests, editor tooling).</summary>
        public void SetGraphAndChosenPath(SpiritImprintGraph newGraph, IReadOnlyList<string> path)
        {
            graph = newGraph;
            chosenPathNodeIds = path == null ? new List<string>() : new List<string>(path);
        }

        void Awake()
        {
            if (graph == null) return;
            if (chosenPathNodeIds == null)
                chosenPathNodeIds = new List<string>();
        }

        void Start()
        {
            TryApplyFromSerializedState();
        }

        void OnDestroy()
        {
            RemoveApplied();
        }

        /// <summary>Re-read serialized path and apply (e.g. after load). Idempotent if path unchanged.</summary>
        public void TryApplyFromSerializedState()
        {
            if (graph == null) return;

            var stats = GetComponent<CharacterStats>();
            if (stats == null)
            {
                Debug.LogWarning($"[SpiritImprint] {name} has no CharacterStats.");
                return;
            }

            if (stats.race != Race.Barbarian)
            {
                Debug.LogWarning($"[SpiritImprint] {name} is {stats.race}; Spirit Imprint applies only to Barbarian.");
                return;
            }

            if (requireSpiritImprintSubsystem && stats.racialSubsystem != RacialSubsystemKind.SpiritImprintBarbarian)
            {
                Debug.LogWarning(
                    $"[SpiritImprint] {name} has racialSubsystem {stats.racialSubsystem}; expected SpiritImprintBarbarian (or disable requireSpiritImprintSubsystem).");
                return;
            }

            var normalized = graph.ValidateAndNormalizePath(chosenPathNodeIds, out var error);
            if (normalized == null)
            {
                Debug.LogWarning($"[SpiritImprint] Invalid path on '{name}': {error}. Falling back to root only.");
                normalized = new List<string> { graph.rootNodeId };
            }

            if (_applied)
                RemoveApplied();

            chosenPathNodeIds = normalized;
            ApplyPath(stats);
            _applied = true;
        }

        void ApplyPath(CharacterStats stats)
        {
            foreach (var nodeId in chosenPathNodeIds)
            {
                if (!graph.TryFindNode(nodeId, out var node))
                    continue;

                if (!_modifierSources.TryGetValue(nodeId, out var src))
                {
                    src = new SpiritImprintNodeModifierSource(graph, nodeId);
                    _modifierSources[nodeId] = src;
                }

                if (node.statModifiers != null)
                {
                    foreach (var mod in node.statModifiers)
                    {
                        var targetStat = stats.GetStatByType(mod.attribute);
                        targetStat?.AddModifier(mod.value, src);
                    }
                }

                if (node.resistanceModifiers != null)
                {
                    foreach (var res in node.resistanceModifiers)
                        stats.AddResistanceModifier(res.type, res.value, src);
                }

                if (node.passiveEffects != null)
                {
                    foreach (var passive in node.passiveEffects)
                        passive?.OnApply(gameObject);
                }
            }
        }

        void RemoveApplied()
        {
            if (!_applied || graph == null) return;

            var stats = GetComponent<CharacterStats>();
            foreach (var nodeId in chosenPathNodeIds)
            {
                if (!graph.TryFindNode(nodeId, out var node)) continue;

                if (stats != null && _modifierSources.TryGetValue(nodeId, out var src))
                {
                    if (node.statModifiers != null)
                    {
                        foreach (var mod in node.statModifiers)
                        {
                            var targetStat = stats.GetStatByType(mod.attribute);
                            targetStat?.RemoveModifiersFromSource(src);
                        }
                    }

                    if (node.resistanceModifiers != null)
                    {
                        foreach (var res in node.resistanceModifiers)
                            stats.RemoveResistanceModifier(res.type, src);
                    }
                }

                if (node.passiveEffects != null)
                {
                    for (var i = node.passiveEffects.Count - 1; i >= 0; i--)
                        node.passiveEffects[i]?.OnRemove(gameObject);
                }
            }

            _modifierSources.Clear();
            _applied = false;
        }

        public void RefreshPassives()
        {
            if (!_applied || graph == null) return;
            foreach (var nodeId in chosenPathNodeIds)
            {
                if (!graph.TryFindNode(nodeId, out var node) || node.passiveEffects == null) continue;
                foreach (var passive in node.passiveEffects)
                    passive?.Refresh(gameObject);
            }
        }

        public void NotifyPassivesTurnStart()
        {
            if (!_applied || graph == null) return;
            foreach (var nodeId in chosenPathNodeIds)
            {
                if (!graph.TryFindNode(nodeId, out var node) || node.passiveEffects == null) continue;
                foreach (var passive in node.passiveEffects)
                    passive?.OnTurnStart(gameObject);
            }
        }

        /// <summary>Append one child of the current leaf if valid (single-node advance).</summary>
        public bool TryAppendChild(string childNodeId, out string failureReason)
        {
            failureReason = null;
            if (graph == null)
            {
                failureReason = "Graph is null.";
                return false;
            }

            if (chosenPathNodeIds == null || chosenPathNodeIds.Count == 0)
                chosenPathNodeIds = new List<string> { graph.rootNodeId };

            string tail = chosenPathNodeIds[chosenPathNodeIds.Count - 1];
            if (!graph.TryFindNode(childNodeId, out SpiritImprintNodeData child))
            {
                failureReason = $"Unknown child id '{childNodeId}'.";
                return false;
            }

            if (child.parentNodeId != tail)
            {
                failureReason = $"'{childNodeId}' is not a direct child of tail '{tail}'.";
                return false;
            }

            var trial = new List<string>(chosenPathNodeIds) { childNodeId };
            if (graph.ValidateAndNormalizePath(trial, out string err) == null)
            {
                failureReason = err;
                return false;
            }

            chosenPathNodeIds = trial;
            TryApplyFromSerializedState();
            return true;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>Dev/test: replace path and re-apply. Stripped from non-development player builds.</summary>
        public void DevSetPathAndReapply(IReadOnlyList<string> newPath)
        {
            if (graph == null) return;
            chosenPathNodeIds = newPath == null ? new List<string>() : new List<string>(newPath);
            TryApplyFromSerializedState();
        }

        /// <summary>Dev/test alias for <see cref="TryAppendChild"/>.</summary>
        public bool DevTryAppendChild(string childNodeId, out string failureReason) =>
            TryAppendChild(childNodeId, out failureReason);
#endif
    }
}
