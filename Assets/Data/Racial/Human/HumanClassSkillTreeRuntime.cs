using System;
using System.Collections.Generic;
using JRogue.Stats;
using JRogue.Stats.Racial;
using UnityEngine;

namespace JRogue.Racial
{
    /// <summary>
    /// D2-style human class skill tree (Knight or Priest). Applies per-rank stat modifiers from preset ranks.
    /// </summary>
    [DefaultExecutionOrder(52)]
    public class HumanClassSkillTreeRuntime : MonoBehaviour
    {
        [SerializeField] HumanClassSkillTreeDefinition skillTree;
        [SerializeField] int skillPointsTotal = 10;
        [SerializeField] List<HumanSkillNodeRankEntry> presetNodeRanks = new List<HumanSkillNodeRankEntry>();

        readonly Dictionary<string, int> _ranksByNodeId = new Dictionary<string, int>(StringComparer.Ordinal);
        readonly Dictionary<string, HumanClassSkillNodeModifierSource> _sources =
            new Dictionary<string, HumanClassSkillNodeModifierSource>(StringComparer.Ordinal);

        readonly HashSet<string> _appliedNodeIds = new HashSet<string>(StringComparer.Ordinal);

        CharacterStats _stats;
        bool _applied;

        public HumanClassSkillTreeDefinition SkillTree => skillTree;

        public void SetSkillTreeAndRanks(
            HumanClassSkillTreeDefinition tree,
            int pointsTotal,
            IReadOnlyList<HumanSkillNodeRankEntry> ranks)
        {
            skillTree = tree;
            skillPointsTotal = pointsTotal;
            presetNodeRanks = ranks == null
                ? new List<HumanSkillNodeRankEntry>()
                : new List<HumanSkillNodeRankEntry>(ranks);
        }

        void Awake() => _stats = GetComponent<CharacterStats>();

        void Start() => TryApplyFromSerializedState();

        void OnDestroy() => RemoveAllApplied();

        public void TryApplyFromSerializedState()
        {
            if (skillTree == null)
                return;

            if (!ValidateActor(out _))
                return;

            LoadRanksFromPreset();
            RemoveAllApplied();
            ApplyAllRankedNodes();
            _applied = true;
        }

        void LoadRanksFromPreset()
        {
            _ranksByNodeId.Clear();
            if (presetNodeRanks == null)
                return;

            foreach (HumanSkillNodeRankEntry entry in presetNodeRanks)
            {
                if (entry == null || string.IsNullOrEmpty(entry.nodeId) || entry.rank < 1)
                    continue;
                _ranksByNodeId[entry.nodeId] = entry.rank;
            }
        }

        void ApplyAllRankedNodes()
        {
            foreach (KeyValuePair<string, int> kv in _ranksByNodeId)
            {
                if (kv.Value > 0)
                    ApplyNode(kv.Key, kv.Value);
            }
        }

        void ApplyNode(string nodeId, int rank)
        {
            if (_appliedNodeIds.Contains(nodeId) || !skillTree.TryFindNode(nodeId, out HumanClassSkillTreeNodeData node))
                return;

            if (!_sources.TryGetValue(nodeId, out HumanClassSkillNodeModifierSource src))
            {
                src = new HumanClassSkillNodeModifierSource(skillTree, nodeId);
                _sources[nodeId] = src;
            }

            HumanClassSkillTreePayloadApplicator.ApplyRankedStats(_stats, src, node, rank);
            _appliedNodeIds.Add(nodeId);
        }

        void RemoveNode(string nodeId)
        {
            if (!_appliedNodeIds.Contains(nodeId) || !skillTree.TryFindNode(nodeId, out HumanClassSkillTreeNodeData node))
                return;

            if (_sources.TryGetValue(nodeId, out HumanClassSkillNodeModifierSource src))
                HumanClassSkillTreePayloadApplicator.RemoveRankedStats(_stats, src, node);

            _appliedNodeIds.Remove(nodeId);
        }

        void RemoveAllApplied()
        {
            if (!_applied)
            {
                _appliedNodeIds.Clear();
                _sources.Clear();
                return;
            }

            foreach (string nodeId in new List<string>(_appliedNodeIds))
                RemoveNode(nodeId);

            _appliedNodeIds.Clear();
            _sources.Clear();
            _applied = false;
        }

        bool ValidateActor(out string failureReason)
        {
            failureReason = null;
            if (_stats == null)
            {
                failureReason = "No CharacterStats.";
                return false;
            }

            if (_stats.race != Race.Human)
            {
                failureReason = $"Actor is {_stats.race}; human class tree requires Human.";
                return false;
            }

            if (skillTree.humanClass != _stats.humanClass)
            {
                failureReason =
                    $"humanClass is {_stats.humanClass}; this tree is for {skillTree.humanClass}.";
                return false;
            }

            if (_stats.racialSubsystem != RacialSubsystemKind.HumanSpecialization)
            {
                failureReason =
                    $"racialSubsystem is {_stats.racialSubsystem}; expected HumanSpecialization.";
                return false;
            }

            return true;
        }
    }
}
