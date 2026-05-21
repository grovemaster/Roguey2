using System;
using System.Collections.Generic;
using JRogue.Stats;
using JRogue.Stats.Racial;
using UnityEngine;

namespace JRogue.Racial
{
    /// <summary>
    /// Undead Diablo-style skill tree: per-node ranks, cluster gates, respec. Payloads are flat while rank &gt;= 1 (v0).
    /// </summary>
    [DefaultExecutionOrder(51)]
    public class UndeadSkillTreeRuntime : MonoBehaviour
    {
        [SerializeField] UndeadSkillTreeDefinition skillTree;
        [SerializeField] int skillPointsTotal = 10;
        [SerializeField] List<UndeadSkillNodeRankEntry> presetNodeRanks = new List<UndeadSkillNodeRankEntry>();
        [SerializeField] bool requireUndeadSkillTreeSubsystem = true;

        readonly Dictionary<string, int> _ranksByNodeId = new Dictionary<string, int>(StringComparer.Ordinal);
        readonly Dictionary<string, UndeadSkillNodeModifierSource> _sources =
            new Dictionary<string, UndeadSkillNodeModifierSource>(StringComparer.Ordinal);

        readonly HashSet<string> _appliedNodeIds = new HashSet<string>(StringComparer.Ordinal);

        CharacterStats _stats;
        bool _applied;

        public UndeadSkillTreeDefinition SkillTree => skillTree;
        public int SkillPointsTotal => skillPointsTotal;
        public int SpentPoints => skillTree != null ? skillTree.GetTotalSpentPoints(_ranksByNodeId) : 0;
        public int UnspentPoints => skillTree != null ? skillTree.GetUnspentPoints(skillPointsTotal, _ranksByNodeId) : 0;

        public IReadOnlyDictionary<string, int> RanksSnapshot => _ranksByNodeId;

        /// <summary>Tests / editor: assign tree, point pool, and preset ranks before <see cref="TryApplyFromSerializedState"/>.</summary>
        public void SetSkillTreeAndRanks(
            UndeadSkillTreeDefinition tree,
            int pointsTotal,
            IReadOnlyList<UndeadSkillNodeRankEntry> ranks)
        {
            skillTree = tree;
            skillPointsTotal = pointsTotal;
            presetNodeRanks = ranks == null
                ? new List<UndeadSkillNodeRankEntry>()
                : new List<UndeadSkillNodeRankEntry>(ranks);
        }

        void Awake() => _stats = GetComponent<CharacterStats>();

        void Start() => TryApplyFromSerializedState();

        void OnDestroy() => RemoveAllApplied();

        public void TryApplyFromSerializedState()
        {
            if (skillTree == null)
                return;

            if (!ValidateUndeadActor(out _))
                return;

            LoadRanksFromPreset();
            RemoveAllApplied();
            ApplyAllRankedNodes();
            _applied = true;
        }

        public bool TrySpendPoint(string nodeId, out string failureReason)
        {
            failureReason = null;
            if (skillTree == null)
            {
                failureReason = "Skill tree is null.";
                return false;
            }

            if (!ValidateUndeadActor(out failureReason))
                return false;

            bool wasRanked = _ranksByNodeId.TryGetValue(nodeId, out int before) && before > 0;
            if (!skillTree.TrySpendPoint(nodeId, skillPointsTotal, _ranksByNodeId, out failureReason))
                return false;

            if (!wasRanked)
                ApplyNode(nodeId);

            return true;
        }

        public bool TryRefundRank(string nodeId, out string failureReason)
        {
            failureReason = null;
            if (skillTree == null)
            {
                failureReason = "Skill tree is null.";
                return false;
            }

            if (!ValidateUndeadActor(out failureReason))
                return false;

            if (!_ranksByNodeId.TryGetValue(nodeId, out int before) || before < 1)
            {
                failureReason = $"Node '{nodeId}' has no rank to refund.";
                return false;
            }

            if (!skillTree.TryRefundRank(nodeId, _ranksByNodeId, out failureReason))
                return false;

            if (before == 1)
                RemoveNode(nodeId);

            return true;
        }

        void LoadRanksFromPreset()
        {
            _ranksByNodeId.Clear();
            if (presetNodeRanks == null)
                return;

            foreach (UndeadSkillNodeRankEntry entry in presetNodeRanks)
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
                    ApplyNode(kv.Key);
            }
        }

        void ApplyNode(string nodeId)
        {
            if (_appliedNodeIds.Contains(nodeId) || !skillTree.TryFindNode(nodeId, out UndeadSkillTreeNodeData node))
                return;

            if (!_sources.TryGetValue(nodeId, out UndeadSkillNodeModifierSource src))
            {
                src = new UndeadSkillNodeModifierSource(skillTree, nodeId);
                _sources[nodeId] = src;
            }

            RacialProgressionPayloadApplicator.Apply(gameObject, _stats, src, node);
            _appliedNodeIds.Add(nodeId);
        }

        void RemoveNode(string nodeId)
        {
            if (!_appliedNodeIds.Contains(nodeId) || !skillTree.TryFindNode(nodeId, out UndeadSkillTreeNodeData node))
                return;

            if (_sources.TryGetValue(nodeId, out UndeadSkillNodeModifierSource src))
                RacialProgressionPayloadApplicator.Remove(gameObject, _stats, src, node);

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

        public void RefreshPassives()
        {
            if (!_applied || skillTree == null)
                return;

            foreach (string nodeId in _appliedNodeIds)
            {
                if (skillTree.TryFindNode(nodeId, out UndeadSkillTreeNodeData node))
                    RacialProgressionPayloadApplicator.RefreshPassives(gameObject, node);
            }
        }

        public void NotifyPassivesTurnStart()
        {
            if (!_applied || skillTree == null)
                return;

            foreach (string nodeId in _appliedNodeIds)
            {
                if (skillTree.TryFindNode(nodeId, out UndeadSkillTreeNodeData node))
                    RacialProgressionPayloadApplicator.NotifyPassivesTurnStart(gameObject, node);
            }
        }

        bool ValidateUndeadActor(out string failureReason)
        {
            failureReason = null;
            if (_stats == null)
            {
                failureReason = "No CharacterStats.";
                return false;
            }

            if (_stats.race != Race.Undead)
            {
                failureReason = $"Actor is {_stats.race}; Undead skill tree requires Undead.";
                return false;
            }

            if (requireUndeadSkillTreeSubsystem && _stats.racialSubsystem != RacialSubsystemKind.UndeadSkillTree)
            {
                failureReason =
                    $"racialSubsystem is {_stats.racialSubsystem}; expected UndeadSkillTree (or disable requireUndeadSkillTreeSubsystem).";
                return false;
            }

            return true;
        }
    }
}
