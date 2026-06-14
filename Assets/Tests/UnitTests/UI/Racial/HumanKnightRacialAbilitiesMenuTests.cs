using System.Collections.Generic;
using JRogue.Ability.Knight;
using JRogue.Actors;
using JRogue.Racial;
using JRogue.Stats;
using JRogue.Stats.Racial;
using JRogue.UI.Racial;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UI.Racial
{
    [TestFixture]
    public sealed class HumanKnightRacialAbilitiesMenuTests
    {
        readonly List<GameObject> _created = new();
        readonly List<Object> _assets = new();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _created)
            {
                if (go != null)
                    Object.DestroyImmediate(go);
            }

            _created.Clear();

            foreach (Object asset in _assets)
            {
                if (asset != null)
                    Object.DestroyImmediate(asset);
            }

            _assets.Clear();
        }

        [Test]
        public void ResolveBannerText_MatchesEditMode()
        {
            Assert.AreEqual(
                HumanKnightSkillBodyViewModel.EditModeBannerText,
                HumanKnightSkillBodyViewModel.ResolveBannerText(HumanKnightSkillEditMode.Edit));
            Assert.AreEqual(
                HumanKnightSkillBodyViewModel.ViewOnlyDungeonBannerText,
                HumanKnightSkillBodyViewModel.ResolveBannerText(HumanKnightSkillEditMode.ViewOnlyDungeon));
            Assert.AreEqual(
                HumanKnightSkillBodyViewModel.ViewOnlyCombatBannerText,
                HumanKnightSkillBodyViewModel.ResolveBannerText(HumanKnightSkillEditMode.ViewOnlyCombat));
        }

        [Test]
        public void ResolveSelectedNodeId_PrefersRequestedThenFirstRow()
        {
            var sections = new List<HumanKnightSkillBranchSectionModel>
            {
                new()
                {
                    BranchHeader = "GENERAL TECHNIQUES",
                    Rows = new List<HumanKnightSkillRowModel>
                    {
                        new() { NodeId = "knight_passive_might" },
                        new() { NodeId = "knight_aura_valor" },
                    },
                },
            };

            Assert.AreEqual(
                "knight_aura_valor",
                HumanKnightSkillBodyViewModel.ResolveSelectedNodeId("knight_aura_valor", sections));
            Assert.AreEqual(
                "knight_passive_might",
                HumanKnightSkillBodyViewModel.ResolveSelectedNodeId(null, sections));
        }

        [Test]
        public void Build_GroupsRowsAndBuildsSummary()
        {
            BaseActor actor = CreateCommittedKnight(out HumanClassSkillTreeRuntime tree);
            tree.SetSkillTreeAndRanks(
                tree.SkillTree,
                10,
                new[] { new HumanSkillNodeRankEntry { nodeId = "knight_passive_might", rank = 2 } });
            tree.TryApplyFromSerializedState();

            HumanKnightSkillBodyViewModel vm = HumanKnightSkillBodyViewModel.Build(actor);

            Assert.That(vm.SummaryLine, Does.Contain("Points · 8 unspent (2 spent)"));
            Assert.That(vm.SummaryLine, Does.Contain("Level 10"));
            Assert.IsTrue(vm.BranchSections.Count >= 1);
            Assert.AreEqual("knight_passive_might", vm.SelectedNodeId);
            Assert.That(vm.Detail.RankLine, Does.Contain("2 / 5"));
        }

        [Test]
        public void Build_ViewOnlyMode_HidesSpendActionsOnDetail()
        {
            BaseActor actor = CreateCommittedKnight(out HumanClassSkillTreeRuntime tree);

            HumanKnightSkillBodyViewModel vm = HumanKnightSkillBodyViewModel.Build(
                actor,
                "knight_passive_might");

            Assert.AreNotEqual(HumanKnightSkillEditMode.Edit, vm.EditMode);
            Assert.IsFalse(vm.Detail.ShowSpendButton);
            Assert.IsFalse(vm.Detail.ShowAddToHotbarButton);
        }

        [Test]
        public void CompareNodes_OrdersUnlockedBeforeLocked()
        {
            var might = new HumanClassSkillTreeNodeData { nodeId = "might", displayName = "Might" };
            var valor = new HumanClassSkillTreeNodeData { nodeId = "valor", displayName = "Valor" };
            var ranks = new Dictionary<string, int> { { "valor", 2 } };

            Assert.Less(
                HumanKnightSkillBodyViewModel.CompareNodes(valor, might, ranks),
                0);
        }

        BaseActor CreateCommittedKnight(out HumanClassSkillTreeRuntime treeRuntime)
        {
            GameObject go = new GameObject("KnightMenuTest");
            _created.Add(go);
            BaseActor actor = go.AddComponent<BaseActor>();
            var stats = go.AddComponent<CharacterStats>();
            stats.race = Race.Human;
            stats.humanClass = HumanClass.None;
            stats.racialSubsystem = RacialSubsystemKind.HumanSpecialization;
            stats.level = 10;
            stats.currentSoulPower = 18;

            treeRuntime = go.AddComponent<HumanClassSkillTreeRuntime>();
            treeRuntime.SetSkillTreeAndRanks(CreateSampleTree(), 10, new List<HumanSkillNodeRankEntry>());

            HumanClassCommitment.TryCommit(go, HumanClass.Knight, out _);
            go.AddComponent<KnightSkillMasteryRuntime>();
            go.AddComponent<KnightAuraStateRuntime>();
            return actor;
        }

        HumanClassSkillTreeDefinition CreateSampleTree()
        {
            var tree = ScriptableObject.CreateInstance<HumanClassSkillTreeDefinition>();
            tree.humanClass = HumanClass.Knight;

            var might = new HumanClassSkillTreeNodeData
            {
                nodeId = "knight_passive_might",
                displayName = "Iron Posture",
                branch = "general",
                maxRanks = 5,
                requiredCharacterLevel = 1,
                tags = new List<KnightSkillTag> { KnightSkillTag.Passive },
            };

            var valorToggle = ScriptableObject.CreateInstance<KnightAuraToggleAbility>();
            valorToggle.knightSkillId = "knight_aura_valor";
            _assets.Add(valorToggle);

            var valor = new HumanClassSkillTreeNodeData
            {
                nodeId = "knight_aura_valor",
                displayName = "Valor Aura",
                branch = "valor",
                maxRanks = 5,
                requiredCharacterLevel = 3,
                requiredParentNodeId = "knight_passive_might",
                requiredParentMinRank = 1,
                tags = new List<KnightSkillTag>
                {
                    KnightSkillTag.Aura,
                    KnightSkillTag.AuraStance,
                    KnightSkillTag.AuraToggle,
                },
                activeAbilities = new List<Ability.AbilityAction> { valorToggle },
            };

            tree.nodes = new List<HumanClassSkillTreeNodeData> { might, valor };
            _assets.Add(tree);
            return tree;
        }
    }
}
