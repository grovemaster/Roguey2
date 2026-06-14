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
        public int SkillPointsTotal => skillPointsTotal;
        public int SpentPoints => skillTree != null ? skillTree.GetTotalSpentPoints(_ranksByNodeId) : 0;
        public int UnspentPoints => skillTree != null ? skillTree.GetUnspentPoints(skillPointsTotal, _ranksByNodeId) : 0;
        public IReadOnlyDictionary<string, int> RanksSnapshot => _ranksByNodeId;

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

        public int GetRank(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
                return 0;

            return _ranksByNodeId.TryGetValue(nodeId, out int rank) ? rank : 0;
        }

        public bool TrySpendPoint(string nodeId, out string failureReason)
        {
            failureReason = null;
            if (skillTree == null)
            {
                failureReason = "Skill tree is null.";
                return false;
            }

            if (!ValidateActor(out failureReason))
                return false;

            int before = GetRank(nodeId);
            if (!skillTree.TrySpendPoint(
                    nodeId,
                    skillPointsTotal,
                    _stats.level,
                    _ranksByNodeId,
                    out failureReason))
            {
                return false;
            }

            int after = GetRank(nodeId);
            if (after != before)
                SetNodeRank(nodeId, after);

            SyncPresetEntry(nodeId, after);
            return true;
        }

        public bool TryIncrementRankFromCombat(string nodeId, out string failureReason)
        {
            failureReason = null;
            if (skillTree == null)
            {
                failureReason = "Skill tree is null.";
                return false;
            }

            if (!ValidateActor(out failureReason))
                return false;

            int before = GetRank(nodeId);
            if (!skillTree.TryIncrementRankFromCombat(
                    nodeId,
                    _stats.level,
                    _ranksByNodeId,
                    out failureReason))
            {
                return false;
            }

            int after = GetRank(nodeId);
            if (after != before)
                SetNodeRank(nodeId, after);

            SyncPresetEntry(nodeId, after);
            return true;
        }

        void SyncPresetEntry(string nodeId, int rank)
        {
            presetNodeRanks ??= new List<HumanSkillNodeRankEntry>();
            for (int i = 0; i < presetNodeRanks.Count; i++)
            {
                HumanSkillNodeRankEntry entry = presetNodeRanks[i];
                if (entry == null || entry.nodeId != nodeId)
                    continue;

                entry.rank = rank;
                return;
            }

            presetNodeRanks.Add(new HumanSkillNodeRankEntry { nodeId = nodeId, rank = rank });
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
                    SetNodeRank(kv.Key, kv.Value);
            }
        }

        void SetNodeRank(string nodeId, int rank)
        {
            if (rank < 1)
            {
                RemoveNode(nodeId);
                return;
            }

            if (!skillTree.TryFindNode(nodeId, out HumanClassSkillTreeNodeData node))
                return;

            if (!_sources.TryGetValue(nodeId, out HumanClassSkillNodeModifierSource src))
            {
                src = new HumanClassSkillNodeModifierSource(skillTree, nodeId);
                _sources[nodeId] = src;
            }

            HumanClassSkillTreePayloadApplicator.ApplyRankedStats(_stats, src, node, rank);
            _appliedNodeIds.Add(nodeId);
            _ranksByNodeId[nodeId] = rank;
        }

        void RemoveNode(string nodeId)
        {
            if (!_appliedNodeIds.Contains(nodeId) || !skillTree.TryFindNode(nodeId, out HumanClassSkillTreeNodeData node))
                return;

            if (_sources.TryGetValue(nodeId, out HumanClassSkillNodeModifierSource src))
                HumanClassSkillTreePayloadApplicator.RemoveRankedStats(_stats, src, node);

            _appliedNodeIds.Remove(nodeId);
            _ranksByNodeId.Remove(nodeId);
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
