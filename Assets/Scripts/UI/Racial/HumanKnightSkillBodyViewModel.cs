using System;
using System.Collections.Generic;
using JRogue.Ability;
using JRogue.Actors;
using JRogue.Racial;
using JRogue.Stats;
using JRogue.Stats.Racial;
using JRogue.UI.Hotbar;
using JRogue.World.Generation;
using UnityEngine;

namespace JRogue.UI.Racial
{
    public enum HumanKnightSkillEditMode
    {
        Edit,
        ViewOnlyDungeon,
        ViewOnlyCombat,
    }

    public sealed class HumanKnightSkillBranchSectionModel
    {
        public string BranchHeader = string.Empty;
        public List<HumanKnightSkillRowModel> Rows = new();
    }

    public sealed class HumanKnightSkillRowModel
    {
        public string NodeId = string.Empty;
        public HumanClassSkillTreeNodeData Node;
        public string Title = string.Empty;
        public int Rank;
        public int MaxRanks;
        public string RankLabel = string.Empty;
        public bool ShowRankProficiencyBar;
        public float RankProficiencyFraction;
        public string RankProficiencyLabel = string.Empty;
        public bool ShowMastery;
        public string MasteryLabel = string.Empty;
        public float MasteryFraction;
        public bool ShowActiveBadge;
        public bool ShowLockedBadge;
        public bool ShowMaxBadge;
    }

    public sealed class HumanKnightSkillDetailModel
    {
        public string NodeId = string.Empty;
        public string Title = string.Empty;
        public string Description = string.Empty;
        public string RankLine = string.Empty;
        public string ProficiencyLine = string.Empty;
        public string MasteryLine = string.Empty;
        public string GateReason = string.Empty;
        public bool ShowSpendButton;
        public bool SpendEnabled;
        public string SpendDisabledReason = string.Empty;
        public bool ShowAddToHotbarButton;
        public bool AddToHotbarEnabled;
        public string AddToHotbarDisabledReason = string.Empty;
        public string HotbarFootnote =
            "Assign unlocked Knight actives on the <b>ability hotbar</b> to use them in combat.";
        public string ProficienciesFootnote =
            "Full mastery list also on the <b>P</b> proficiencies menu.";
    }

    public sealed class HumanKnightSkillBodyViewModel
    {
        public const string MissingRuntimeMessage =
            "Knight skill data is missing for this character.";

        public const string UncommittedClassMessage =
            "This character has not committed to a class path. "
            + "Visit a <b>drill instructor</b> or <b>Mage tutor</b> in town to begin your specialization.";

        public const string EditModeBannerText =
            "Spend skill points on your Knight techniques here. New points come from <b>training</b> "
            + "with masters in the field — visit a <b>drill instructor</b> when available.";

        public const string ViewOnlyDungeonBannerText =
            "View only — you can only spend skill points in town.";

        public const string ViewOnlyCombatBannerText =
            "View only — finish combat before adjusting your skill tree.";

        public const string SummaryFootnote =
            "Rank rises from <b>skill points</b> in town or <b>proficiency experience</b> from using "
            + "<b>actives</b> in combat. Mastery grows from the same combat use.";

        public const string TreeEmptyMessage =
            "No Knight skills are defined for this tree.";

        static readonly string[] BranchOrder =
        {
            "general",
            "bulwark",
            "valor",
            "command",
        };

        public HumanKnightSkillEditMode EditMode = HumanKnightSkillEditMode.ViewOnlyDungeon;
        public string BannerText = string.Empty;
        public string SummaryLine = string.Empty;
        public List<HumanKnightSkillBranchSectionModel> BranchSections = new();
        public string SelectedNodeId = string.Empty;
        public HumanKnightSkillDetailModel Detail = new();

        public static HumanKnightSkillBodyViewModel Build(
            BaseActor knight,
            string selectedNodeId = null)
        {
            var vm = new HumanKnightSkillBodyViewModel();
            if (knight == null)
                return vm;

            HumanClassSkillTreeRuntime treeRuntime = knight.GetComponent<HumanClassSkillTreeRuntime>();
            CharacterStats stats = knight.stats;
            if (treeRuntime == null || stats == null || treeRuntime.SkillTree == null)
                return vm;

            HumanClassSkillTreeDefinition tree = treeRuntime.SkillTree;
            KnightSkillMasteryRuntime masteryRuntime = knight.GetComponent<KnightSkillMasteryRuntime>();
            KnightAuraStateRuntime auraState = knight.GetComponent<KnightAuraStateRuntime>();
            IReadOnlyDictionary<string, int> ranks = treeRuntime.RanksSnapshot;

            vm.EditMode = ResolveEditMode();
            vm.BannerText = ResolveBannerText(vm.EditMode);
            vm.SummaryLine = BuildSummaryLine(stats, treeRuntime, auraState, tree, ranks);
            vm.BranchSections = BuildBranchSections(
                tree,
                ranks,
                stats.level,
                treeRuntime.SkillPointsTotal,
                masteryRuntime,
                auraState);
            vm.SelectedNodeId = ResolveSelectedNodeId(selectedNodeId, vm.BranchSections);
            vm.Detail = BuildDetail(
                vm.SelectedNodeId,
                tree,
                treeRuntime,
                stats,
                masteryRuntime,
                auraState,
                knight,
                vm.EditMode == HumanKnightSkillEditMode.Edit);
            return vm;
        }

        public static HumanKnightSkillEditMode ResolveEditMode()
        {
            if (SafeZonePolicyService.TryAllowHumanKnightSkillSpend(out _, logDeny: false))
                return HumanKnightSkillEditMode.Edit;

            if (Manager.Combat.CombatThreatCoordinator.Instance != null
                && Manager.Combat.CombatThreatCoordinator.Instance.IsInCombat)
            {
                return HumanKnightSkillEditMode.ViewOnlyCombat;
            }

            return HumanKnightSkillEditMode.ViewOnlyDungeon;
        }

        public static string ResolveBannerText(HumanKnightSkillEditMode mode) =>
            mode switch
            {
                HumanKnightSkillEditMode.Edit => EditModeBannerText,
                HumanKnightSkillEditMode.ViewOnlyCombat => ViewOnlyCombatBannerText,
                _ => ViewOnlyDungeonBannerText,
            };

        public static string ResolveSelectedNodeId(
            string requestedNodeId,
            IReadOnlyList<HumanKnightSkillBranchSectionModel> sections)
        {
            if (!string.IsNullOrWhiteSpace(requestedNodeId))
            {
                string trimmed = requestedNodeId.Trim();
                if (ContainsNodeId(sections, trimmed))
                    return trimmed;
            }

            if (sections == null)
                return string.Empty;

            for (int i = 0; i < sections.Count; i++)
            {
                HumanKnightSkillBranchSectionModel section = sections[i];
                if (section?.Rows == null || section.Rows.Count == 0)
                    continue;

                return section.Rows[0].NodeId;
            }

            return string.Empty;
        }

        static bool ContainsNodeId(IReadOnlyList<HumanKnightSkillBranchSectionModel> sections, string nodeId)
        {
            if (sections == null || string.IsNullOrWhiteSpace(nodeId))
                return false;

            for (int i = 0; i < sections.Count; i++)
            {
                HumanKnightSkillBranchSectionModel section = sections[i];
                if (section?.Rows == null)
                    continue;

                for (int r = 0; r < section.Rows.Count; r++)
                {
                    if (string.Equals(section.Rows[r]?.NodeId, nodeId, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            return false;
        }

        static string BuildSummaryLine(
            CharacterStats stats,
            HumanClassSkillTreeRuntime treeRuntime,
            KnightAuraStateRuntime auraState,
            HumanClassSkillTreeDefinition tree,
            IReadOnlyDictionary<string, int> ranks)
        {
            int currentSoul = stats != null ? stats.currentSoulPower : 0;
            int maxSoul = stats != null ? stats.MaxSoulPower : 0;
            int unspent = treeRuntime.UnspentPoints;
            int spent = treeRuntime.SpentPoints;
            int level = stats != null ? stats.level : 1;
            int trainingCap = KnightSkillProgressionRules.GetTrainingCap(level);
            string stanceName = ResolveStanceName(auraState, tree);

            return
                $"SOUL POWER · {currentSoul} / {maxSoul} · "
                + $"Points · {unspent} unspent ({spent} spent) · "
                + $"Stance · {stanceName} · "
                + $"Level {level} · Mastery cap {trainingCap}";
        }

        static string ResolveStanceName(KnightAuraStateRuntime auraState, HumanClassSkillTreeDefinition tree)
        {
            if (auraState == null || tree == null || string.IsNullOrEmpty(auraState.ActiveStanceNodeId))
                return "—";

            if (tree.TryFindNode(auraState.ActiveStanceNodeId, out HumanClassSkillTreeNodeData node))
                return ResolveTitle(node);

            return auraState.ActiveStanceNodeId;
        }

        static List<HumanKnightSkillBranchSectionModel> BuildBranchSections(
            HumanClassSkillTreeDefinition tree,
            IReadOnlyDictionary<string, int> ranks,
            int characterLevel,
            int skillPointsTotal,
            KnightSkillMasteryRuntime masteryRuntime,
            KnightAuraStateRuntime auraState)
        {
            var byBranch = new Dictionary<string, List<HumanClassSkillTreeNodeData>>(StringComparer.OrdinalIgnoreCase);
            if (tree.nodes != null)
            {
                for (int i = 0; i < tree.nodes.Count; i++)
                {
                    HumanClassSkillTreeNodeData node = tree.nodes[i];
                    if (node == null || string.IsNullOrEmpty(node.nodeId))
                        continue;

                    string branchKey = NormalizeBranchKey(node.branch);
                    if (!byBranch.TryGetValue(branchKey, out List<HumanClassSkillTreeNodeData> list))
                    {
                        list = new List<HumanClassSkillTreeNodeData>();
                        byBranch[branchKey] = list;
                    }

                    list.Add(node);
                }
            }

            var sections = new List<HumanKnightSkillBranchSectionModel>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < BranchOrder.Length; i++)
            {
                string branchKey = BranchOrder[i];
                if (!byBranch.TryGetValue(branchKey, out List<HumanClassSkillTreeNodeData> nodes))
                    continue;

                seen.Add(branchKey);
                sections.Add(BuildSection(branchKey, nodes, ranks, characterLevel, skillPointsTotal, masteryRuntime, auraState, tree));
            }

            foreach (KeyValuePair<string, List<HumanClassSkillTreeNodeData>> kv in byBranch)
            {
                if (seen.Contains(kv.Key))
                    continue;

                sections.Add(BuildSection(kv.Key, kv.Value, ranks, characterLevel, skillPointsTotal, masteryRuntime, auraState, tree));
            }

            return sections;
        }

        static HumanKnightSkillBranchSectionModel BuildSection(
            string branchKey,
            List<HumanClassSkillTreeNodeData> nodes,
            IReadOnlyDictionary<string, int> ranks,
            int characterLevel,
            int skillPointsTotal,
            KnightSkillMasteryRuntime masteryRuntime,
            KnightAuraStateRuntime auraState,
            HumanClassSkillTreeDefinition tree)
        {
            nodes.Sort((left, right) => CompareNodes(left, right, ranks));

            var section = new HumanKnightSkillBranchSectionModel
            {
                BranchHeader = ResolveBranchHeader(branchKey),
            };

            for (int i = 0; i < nodes.Count; i++)
            {
                section.Rows.Add(BuildRow(
                    nodes[i],
                    ranks,
                    characterLevel,
                    skillPointsTotal,
                    masteryRuntime,
                    auraState,
                    tree));
            }

            return section;
        }

        public static int CompareNodes(
            HumanClassSkillTreeNodeData left,
            HumanClassSkillTreeNodeData right,
            IReadOnlyDictionary<string, int> ranks)
        {
            int leftRank = GetRank(left, ranks);
            int rightRank = GetRank(right, ranks);

            bool leftUnlocked = leftRank >= 1;
            bool rightUnlocked = rightRank >= 1;
            if (leftUnlocked != rightUnlocked)
                return rightUnlocked.CompareTo(leftUnlocked);

            if (leftRank != rightRank)
                return rightRank.CompareTo(leftRank);

            if (!leftUnlocked && !rightUnlocked)
            {
                int levelCompare = left.requiredCharacterLevel.CompareTo(right.requiredCharacterLevel);
                if (levelCompare != 0)
                    return levelCompare;
            }

            return string.Compare(
                ResolveTitle(left),
                ResolveTitle(right),
                StringComparison.OrdinalIgnoreCase);
        }

        static HumanKnightSkillRowModel BuildRow(
            HumanClassSkillTreeNodeData node,
            IReadOnlyDictionary<string, int> ranks,
            int characterLevel,
            int skillPointsTotal,
            KnightSkillMasteryRuntime masteryRuntime,
            KnightAuraStateRuntime auraState,
            HumanClassSkillTreeDefinition tree)
        {
            int rank = GetRank(node, ranks);
            int maxRanks = node.maxRanks;
            string masteryId = node.ResolveMasteryId();
            int trainingCap = KnightSkillProgressionRules.GetTrainingCap(characterLevel);

            bool canSpend = tree.ValidateSpendPoint(
                node.nodeId,
                skillPointsTotal,
                characterLevel,
                ranks,
                out _);

            var row = new HumanKnightSkillRowModel
            {
                Node = node,
                NodeId = node.nodeId,
                Title = ResolveTitle(node),
                Rank = rank,
                MaxRanks = maxRanks,
                RankLabel = $"{rank} / {maxRanks}",
                ShowActiveBadge = auraState != null && auraState.IsActive(node.nodeId),
                ShowLockedBadge = rank < 1 && !canSpend,
                ShowMaxBadge = rank >= maxRanks,
            };

            if (rank >= 1 && node.HasActiveAbilities && rank < maxRanks)
            {
                int rankPxp = masteryRuntime != null ? masteryRuntime.GetRankPxp(masteryId) : 0;
                int xpToNext = KnightSkillProgressionRules.GetXpToNextRank(rank);
                row.ShowRankProficiencyBar = true;
                row.RankProficiencyFraction = xpToNext > 0 ? Mathf.Clamp01((float)rankPxp / xpToNext) : 0f;
                row.RankProficiencyLabel = $"Proficiency {rankPxp} / {xpToNext}";
            }

            if (rank >= 1)
            {
                int masteryLevel = masteryRuntime != null ? masteryRuntime.GetMasteryLevel(masteryId) : 0;
                int masteryPxp = masteryRuntime != null ? masteryRuntime.GetMasteryPxp(masteryId) : 0;
                int xpToNextMastery = KnightSkillProgressionRules.GetXpToNextMastery(masteryLevel);
                row.ShowMastery = true;
                row.MasteryLabel = $"Mastery {masteryLevel} / {trainingCap}";
                row.MasteryFraction = xpToNextMastery > 0 && masteryLevel < trainingCap
                    ? Mathf.Clamp01((float)masteryPxp / xpToNextMastery)
                    : 0f;
            }

            return row;
        }

        static HumanKnightSkillDetailModel BuildDetail(
            string nodeId,
            HumanClassSkillTreeDefinition tree,
            HumanClassSkillTreeRuntime treeRuntime,
            CharacterStats stats,
            KnightSkillMasteryRuntime masteryRuntime,
            KnightAuraStateRuntime auraState,
            BaseActor actor,
            bool editMode)
        {
            var detail = new HumanKnightSkillDetailModel();
            if (string.IsNullOrWhiteSpace(nodeId) || !tree.TryFindNode(nodeId, out HumanClassSkillTreeNodeData node))
                return detail;

            IReadOnlyDictionary<string, int> ranks = treeRuntime.RanksSnapshot;
            int rank = GetRank(node, ranks);
            int maxRanks = node.maxRanks;
            string masteryId = node.ResolveMasteryId();
            int trainingCap = KnightSkillProgressionRules.GetTrainingCap(stats.level);

            detail.NodeId = node.nodeId;
            detail.Title = ResolveTitle(node);
            detail.Description = string.IsNullOrWhiteSpace(node.description)
                ? "—"
                : node.description.Trim();
            detail.RankLine = $"Rank: {rank} / {maxRanks}";

            if (rank >= 1 && node.HasActiveAbilities && rank < maxRanks)
            {
                int rankPxp = masteryRuntime != null ? masteryRuntime.GetRankPxp(masteryId) : 0;
                int xpToNext = KnightSkillProgressionRules.GetXpToNextRank(rank);
                detail.ProficiencyLine = $"Proficiency toward next rank: {rankPxp} / {xpToNext}";
            }

            if (rank >= 1)
            {
                int masteryLevel = masteryRuntime != null ? masteryRuntime.GetMasteryLevel(masteryId) : 0;
                int masteryPxp = masteryRuntime != null ? masteryRuntime.GetMasteryPxp(masteryId) : 0;
                int xpToNextMastery = KnightSkillProgressionRules.GetXpToNextMastery(masteryLevel);
                detail.MasteryLine =
                    $"Mastery: {masteryLevel} / {trainingCap} · Practice {masteryPxp} / {xpToNextMastery}";
            }

            if (rank < 1)
            {
                if (tree.ValidateSpendPoint(
                        node.nodeId,
                        treeRuntime.SkillPointsTotal,
                        stats.level,
                        ranks,
                        out string spendReason))
                {
                    detail.GateReason = "Eligible for first rank.";
                }
                else
                {
                    detail.GateReason = spendReason ?? "Locked.";
                }
            }
            else if (rank >= maxRanks)
            {
                detail.GateReason = "Maximum rank reached.";
            }
            else if (auraState != null && auraState.IsActive(node.nodeId))
            {
                detail.GateReason = "Aura is active.";
            }

            if (!editMode)
                return detail;

            detail.ShowSpendButton = rank < maxRanks;
            if (detail.ShowSpendButton)
            {
                if (tree.ValidateSpendPoint(
                        node.nodeId,
                        treeRuntime.SkillPointsTotal,
                        stats.level,
                        ranks,
                        out string failureReason))
                {
                    detail.SpendEnabled = true;
                }
                else
                {
                    detail.SpendEnabled = false;
                    detail.SpendDisabledReason = failureReason ?? "Cannot spend skill point.";
                }
            }

            if (rank >= 1 && node.HasActiveAbilities)
                PopulateHotbarAddState(detail, node, actor);

            return detail;
        }

        static void PopulateHotbarAddState(
            HumanKnightSkillDetailModel detail,
            HumanClassSkillTreeNodeData node,
            BaseActor actor)
        {
            detail.ShowAddToHotbarButton = true;
            if (actor == null)
            {
                detail.AddToHotbarEnabled = false;
                detail.AddToHotbarDisabledReason = "No actor.";
                return;
            }

            int abilityIndex = Mathf.Clamp(node.activeAbilityIndex, 0, node.activeAbilities.Count - 1);
            HotbarLayout layout = actor.GetComponent<HotbarLayout>();
            if (layout == null)
            {
                detail.AddToHotbarEnabled = true;
                return;
            }

            for (int slot = 0; slot < HotbarLayout.HotbarMainSlotCount; slot++)
            {
                HotbarEntry entry = layout.GetSlot(slot);
                if (entry.Kind == HotbarEntryKind.HumanKnightSkill
                    && string.Equals(entry.knightNodeId, node.nodeId, StringComparison.Ordinal)
                    && entry.abilityIndex == abilityIndex)
                {
                    detail.AddToHotbarEnabled = false;
                    detail.AddToHotbarDisabledReason = "Already on hotbar.";
                    return;
                }
            }

            for (int slot = 0; slot < HotbarLayout.HotbarMainSlotCount; slot++)
            {
                if (layout.GetSlot(slot).IsEmpty())
                {
                    detail.AddToHotbarEnabled = true;
                    return;
                }
            }

            detail.AddToHotbarEnabled = false;
            detail.AddToHotbarDisabledReason = "Hotbar full — open ability hotbar to rearrange.";
        }

        static int GetRank(HumanClassSkillTreeNodeData node, IReadOnlyDictionary<string, int> ranks)
        {
            if (node == null || ranks == null || string.IsNullOrEmpty(node.nodeId))
                return 0;

            return ranks.TryGetValue(node.nodeId, out int rank) ? rank : 0;
        }

        static string NormalizeBranchKey(string branch)
        {
            if (string.IsNullOrWhiteSpace(branch))
                return "general";

            return branch.Trim().ToLowerInvariant();
        }

        static string ResolveBranchHeader(string branchKey) =>
            branchKey switch
            {
                "general" => "GENERAL TECHNIQUES",
                "bulwark" => "BULWARK",
                "valor" => "VALOR",
                "command" => "COMMAND",
                _ => branchKey.ToUpperInvariant(),
            };

        public static string ResolveTitle(HumanClassSkillTreeNodeData node)
        {
            if (node == null)
                return string.Empty;

            if (!string.IsNullOrWhiteSpace(node.displayName))
                return node.displayName.Trim();

            return string.IsNullOrWhiteSpace(node.nodeId) ? "Skill" : node.nodeId.Trim();
        }
    }
}
