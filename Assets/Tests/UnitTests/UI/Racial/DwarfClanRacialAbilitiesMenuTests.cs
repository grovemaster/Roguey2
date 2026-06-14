using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Item.Essence;
using JRogue.Racial;
using JRogue.Stats;
using JRogue.Stats.Racial;
using JRogue.UI.Racial;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UI.Racial
{
    [TestFixture]
    public sealed class DwarfClanRacialAbilitiesMenuTests
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
        public void FormatMemberBanner_UsesClanShortName()
        {
            Assert.AreEqual(
                "View only — learn new clan techniques at the Hall of Ancestors altar in Forge Brothers.",
                DwarfClanBodyViewModel.FormatMemberBanner("Forge Brothers"));
        }

        [Test]
        public void Build_Unaffiliated_ShowsBannerAndEmptyCommonSlots()
        {
            BaseActor dwarf = CreateUnaffiliatedDwarf();

            DwarfClanBodyViewModel vm = DwarfClanBodyViewModel.Build(dwarf);

            Assert.IsTrue(vm.CanDisplay);
            Assert.IsTrue(vm.IsUnaffiliated);
            Assert.AreEqual(DwarfClanBodyViewModel.UnaffiliatedBanner, vm.BannerText);
            Assert.AreEqual(DwarfClanBodyViewModel.UnaffiliatedBody, vm.UnaffiliatedMessage);
            Assert.AreEqual(DwarfCommonAbilitiesRuntime.SlotCount, vm.CommonSlots.Count);
            Assert.IsTrue(vm.CommonSlots.TrueForAll(row => row.IsEmpty));
        }

        [Test]
        public void Build_Member_ShowsSummaryAndLearnedNodes()
        {
            DwarfClanDefinition clan = CreateClan();
            BaseActor dwarf = CreateUnaffiliatedDwarf();
            DwarfClanJoinService.ApplyJoinClan(dwarf, clan, out _);

            DwarfClanBodyViewModel vm = DwarfClanBodyViewModel.Build(dwarf);

            Assert.IsFalse(vm.IsUnaffiliated);
            Assert.That(vm.BannerText, Does.Contain("Hall of Ancestors"));
            Assert.That(vm.SummaryLine, Does.Contain("Forge Brothers"));
            Assert.That(vm.SummaryLine, Does.Contain("Rank 0"));
            Assert.That(vm.SummaryLine, Does.Contain("Prestige 5"));
            Assert.That(vm.PatronLine, Does.Contain("Forge Father"));
            Assert.AreEqual(1, vm.ClanCards.Count);
            Assert.AreEqual(SpiritImprintCardKind.Committed, vm.ClanCards[0].Kind);
            Assert.AreEqual("ancestor_root", vm.ClanCards[0].NodeId);
        }

        [Test]
        public void Build_MemberAfterLearn_AppendsTechniqueCard()
        {
            DwarfClanDefinition clan = CreateClan();
            BaseActor dwarf = CreateUnaffiliatedDwarf();
            DwarfClanJoinService.ApplyJoinClan(dwarf, clan, out _);
            DwarfAncestorLearnService.ApplyLearnNode(dwarf, clan, "forge_blessing", out _);

            DwarfClanBodyViewModel vm = DwarfClanBodyViewModel.Build(dwarf);

            Assert.That(vm.SummaryLine, Does.Contain("Rank 1"));
            Assert.AreEqual(2, vm.ClanCards.Count);
            Assert.AreEqual("forge_blessing", vm.ClanCards[1].NodeId);
        }

        [Test]
        public void Build_CommonSlots_ShowInstalledAbility()
        {
            BaseActor dwarf = CreateUnaffiliatedDwarf();
            var common = ScriptableObject.CreateInstance<DwarfCommonAbilityDefinition>();
            common.abilityId = "stone_skin";
            common.displayName = "Stone Skin";
            common.description = "Hardened hide from mountain living.";
            _assets.Add(common);

            DwarfCommonAbilitiesRuntime runtime = dwarf.GetComponent<DwarfCommonAbilitiesRuntime>();
            runtime.SetPresetCommonAbilities(new[]
            {
                new DwarfCommonSlotPreset { slotIndex = 1, ability = common },
            });
            runtime.TryApplyPresetFromSerialized();

            DwarfClanBodyViewModel vm = DwarfClanBodyViewModel.Build(dwarf);

            Assert.AreEqual("Stone Skin", vm.CommonSlots[1].Title);
            Assert.IsFalse(vm.CommonSlots[1].IsEmpty);
            Assert.AreEqual(DwarfClanBodyViewModel.EmptyCommonSlotTitle, vm.CommonSlots[0].Title);
        }

        [Test]
        public void BuildCardsFromLearnedSet_UsesAltarGhostHint()
        {
            SpiritImprintGraph graph = CreateTreeWithExclusiveSibling();

            List<SpiritImprintCardViewModel> cards = BarbarianSpiritImprintViewModel.BuildCardsFromLearnedSet(
                graph,
                new[] { "ancestor_root", "path_a" },
                DwarfClanBodyViewModel.ClanGhostLearnHint);

            Assert.AreEqual(3, cards.Count);
            Assert.AreEqual(SpiritImprintCardKind.ForeclosedGhost, cards[2].Kind);
            Assert.AreEqual(DwarfClanBodyViewModel.ClanGhostLearnHint, cards[2].Description);
        }

        BaseActor CreateUnaffiliatedDwarf()
        {
            GameObject go = new GameObject("DwarfMenuTest");
            _created.Add(go);
            var actor = go.AddComponent<BaseActor>();
            var stats = go.AddComponent<CharacterStats>();
            stats.race = Race.Dwarf;
            stats.racialSubsystem = RacialSubsystemKind.DwarfAncestry;
            stats.level = 5;
            go.AddComponent<DwarfAncestorPathRuntime>();
            go.AddComponent<DwarfClanMembershipRuntime>();
            go.AddComponent<DwarfCommonAbilitiesRuntime>();
            return actor;
        }

        DwarfClanDefinition CreateClan()
        {
            var clan = ScriptableObject.CreateInstance<DwarfClanDefinition>();
            clan.clanId = DwarfClanIds.ForgeBrothersClanId;
            clan.displayName = "Forge Brothers";
            clan.shortName = "Forge Brothers";
            clan.startingPrestige = 5;
            clan.patronAncestor = CreatePatron();
            _assets.Add(clan);
            return clan;
        }

        AncestorDefinition CreatePatron()
        {
            var patron = ScriptableObject.CreateInstance<AncestorDefinition>();
            patron.ancestorId = "forge_father";
            patron.displayName = "Forge Father";
            patron.abilityTree = CreateTree();
            _assets.Add(patron);
            return patron;
        }

        SpiritImprintGraph CreateTree()
        {
            var graph = ScriptableObject.CreateInstance<SpiritImprintGraph>();
            graph.rootNodeId = "ancestor_root";

            var root = new SpiritImprintNodeData { nodeId = "ancestor_root", displayName = "Ancestor Root" };
            var blessing = new SpiritImprintNodeData
            {
                nodeId = "forge_blessing",
                displayName = "Forge Blessing",
                parentNodeId = "ancestor_root",
                requiredCharacterLevel = 1,
                statModifiers = new List<AttributeModifier>
                {
                    new AttributeModifier { attribute = StatType.Strength, value = 1 },
                },
            };

            graph.nodes = new List<SpiritImprintNodeData> { root, blessing };
            _assets.Add(graph);
            return graph;
        }

        SpiritImprintGraph CreateTreeWithExclusiveSibling()
        {
            var graph = ScriptableObject.CreateInstance<SpiritImprintGraph>();
            graph.rootNodeId = "ancestor_root";

            var root = new SpiritImprintNodeData { nodeId = "ancestor_root", displayName = "Root" };
            var pathA = new SpiritImprintNodeData
            {
                nodeId = "path_a",
                displayName = "Path A",
                parentNodeId = "ancestor_root",
                siblingExclusivityGroup = 1,
            };
            var pathB = new SpiritImprintNodeData
            {
                nodeId = "path_b",
                displayName = "Path B",
                parentNodeId = "ancestor_root",
                siblingExclusivityGroup = 1,
            };

            graph.nodes = new List<SpiritImprintNodeData> { root, pathA, pathB };
            _assets.Add(graph);
            return graph;
        }
    }
}
