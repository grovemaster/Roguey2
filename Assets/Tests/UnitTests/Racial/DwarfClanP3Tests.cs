using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Item.Essence;
using JRogue.Racial;
using JRogue.Stats;
using JRogue.Stats.Racial;
using JRogue.UI.Racial;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.Racial
{
    [TestFixture]
    public sealed class DwarfClanP3Tests
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
        public void JoinDifferentClans_SetsDifferentPatrons()
        {
            DwarfClanDefinition forgeBrothers = CreateForgeBrothersClan();
            DwarfClanDefinition stoneWardens = CreateStoneWardensClan();

            BaseActor forgeDwarf = CreateUnaffiliatedDwarf();
            BaseActor stoneDwarf = CreateUnaffiliatedDwarf();

            DwarfClanJoinService.ApplyJoinClan(forgeDwarf, forgeBrothers, out _);
            DwarfClanJoinService.ApplyJoinClan(stoneDwarf, stoneWardens, out _);

            Assert.AreNotEqual(
                forgeDwarf.GetComponent<DwarfAncestorPathRuntime>().PatronAncestor.ancestorId,
                stoneDwarf.GetComponent<DwarfAncestorPathRuntime>().PatronAncestor.ancestorId);
            Assert.AreEqual(DwarfClanIds.ForgeBrothersClanId, forgeDwarf.GetComponent<DwarfClanMembershipRuntime>().ClanId);
            Assert.AreEqual(DwarfClanIds.StoneWardensClanId, stoneDwarf.GetComponent<DwarfClanMembershipRuntime>().ClanId);
        }

        [Test]
        public void Altar_RejectsWrongClanMember()
        {
            DwarfClanDefinition forgeBrothers = CreateForgeBrothersClan();
            DwarfClanDefinition stoneWardens = CreateStoneWardensClan();
            BaseActor dwarf = CreateUnaffiliatedDwarf();
            DwarfClanJoinService.ApplyJoinClan(dwarf, forgeBrothers, out _);

            Assert.IsFalse(DwarfAncestorLearnLogic.IsSpeakerEligibleForAltar(
                dwarf,
                stoneWardens,
                out _,
                out _,
                out string rejectLine));
            Assert.AreEqual(DwarfAncestorLearnLogic.WrongClanMessage, rejectLine);
        }

        [Test]
        public void ExclusiveBranch_FrontierShowsBothChoicesAfterJoin()
        {
            DwarfClanDefinition stoneWardens = CreateStoneWardensClan();
            BaseActor dwarf = CreateUnaffiliatedDwarf();
            DwarfClanJoinService.ApplyJoinClan(dwarf, stoneWardens, out _);

            List<DwarfAncestorFrontierOffer> offers = DwarfAncestorLearnLogic.GetFrontierOffers(dwarf, stoneWardens);

            Assert.AreEqual(2, offers.Count);
            Assert.IsTrue(offers.Exists(o => o.Node.nodeId == "mountain_fist" && o.Selectable));
            Assert.IsTrue(offers.Exists(o => o.Node.nodeId == "earth_sight" && o.Selectable));
        }

        [Test]
        public void ExclusiveBranch_LearningOneForeclosesSiblingOffer()
        {
            DwarfClanDefinition stoneWardens = CreateStoneWardensClan();
            BaseActor dwarf = CreateUnaffiliatedDwarf();
            DwarfClanJoinService.ApplyJoinClan(dwarf, stoneWardens, out _);
            DwarfAncestorLearnService.ApplyLearnNode(dwarf, stoneWardens, "mountain_fist", out _);

            List<DwarfAncestorFrontierOffer> offers = DwarfAncestorLearnLogic.GetFrontierOffers(dwarf, stoneWardens);

            Assert.AreEqual(1, offers.Count);
            Assert.AreEqual("granite_guard", offers[0].Node.nodeId);
            Assert.IsFalse(offers.Exists(o => o.Node.nodeId == "earth_sight"));
        }

        [Test]
        public void ExclusiveBranch_KMenuShowsGhostForForeclosedSibling()
        {
            DwarfClanDefinition stoneWardens = CreateStoneWardensClan();
            BaseActor dwarf = CreateUnaffiliatedDwarf();
            DwarfClanJoinService.ApplyJoinClan(dwarf, stoneWardens, out _);
            DwarfAncestorLearnService.ApplyLearnNode(dwarf, stoneWardens, "mountain_fist", out _);

            DwarfClanBodyViewModel vm = DwarfClanBodyViewModel.Build(dwarf);

            Assert.IsTrue(vm.ClanCards.Exists(
                card => card.Kind == SpiritImprintCardKind.ForeclosedGhost && card.NodeId == "earth_sight"));
            Assert.IsTrue(vm.ClanCards.Exists(
                card => card.Kind == SpiritImprintCardKind.Committed && card.NodeId == "mountain_fist"));
        }

        BaseActor CreateUnaffiliatedDwarf()
        {
            GameObject go = new GameObject("DwarfP3Test");
            _created.Add(go);
            var actor = go.AddComponent<BaseActor>();
            var stats = go.AddComponent<CharacterStats>();
            stats.race = Race.Dwarf;
            stats.racialSubsystem = RacialSubsystemKind.DwarfAncestry;
            stats.level = 5;
            go.AddComponent<DwarfAncestorPathRuntime>();
            go.AddComponent<DwarfClanMembershipRuntime>();
            return actor;
        }

        DwarfClanDefinition CreateForgeBrothersClan()
        {
            var clan = ScriptableObject.CreateInstance<DwarfClanDefinition>();
            clan.clanId = DwarfClanIds.ForgeBrothersClanId;
            clan.displayName = "Forge Brothers";
            clan.shortName = "Forge Brothers";
            clan.startingPrestige = 5;
            clan.patronAncestor = CreatePatron("forge_father", "Forge-Father", CreateLinearTree());
            _assets.Add(clan);
            return clan;
        }

        DwarfClanDefinition CreateStoneWardensClan()
        {
            var clan = ScriptableObject.CreateInstance<DwarfClanDefinition>();
            clan.clanId = DwarfClanIds.StoneWardensClanId;
            clan.displayName = "Stone Wardens";
            clan.shortName = "Stone Wardens";
            clan.startingPrestige = 5;
            clan.patronAncestor = CreatePatron("stone_mother", "Stone Mother", CreateExclusiveBranchTree());
            _assets.Add(clan);
            return clan;
        }

        AncestorDefinition CreatePatron(string ancestorId, string displayName, SpiritImprintGraph tree)
        {
            var patron = ScriptableObject.CreateInstance<AncestorDefinition>();
            patron.ancestorId = ancestorId;
            patron.displayName = displayName;
            patron.abilityTree = tree;
            _assets.Add(patron);
            return patron;
        }

        SpiritImprintGraph CreateLinearTree()
        {
            var graph = ScriptableObject.CreateInstance<SpiritImprintGraph>();
            graph.rootNodeId = "ancestor_root";
            graph.nodes = new List<SpiritImprintNodeData>
            {
                new() { nodeId = "ancestor_root" },
                new()
                {
                    nodeId = "forge_blessing",
                    parentNodeId = "ancestor_root",
                    requiredCharacterLevel = 1,
                },
            };
            _assets.Add(graph);
            return graph;
        }

        SpiritImprintGraph CreateExclusiveBranchTree()
        {
            var graph = ScriptableObject.CreateInstance<SpiritImprintGraph>();
            graph.rootNodeId = "ancestor_root";
            graph.nodes = new List<SpiritImprintNodeData>
            {
                new() { nodeId = "ancestor_root", displayName = "Root" },
                new()
                {
                    nodeId = "mountain_fist",
                    displayName = "Mountain Fist",
                    parentNodeId = "ancestor_root",
                    siblingExclusivityGroup = 1,
                    requiredCharacterLevel = 1,
                },
                new()
                {
                    nodeId = "earth_sight",
                    displayName = "Earth Sight",
                    parentNodeId = "ancestor_root",
                    siblingExclusivityGroup = 1,
                    requiredCharacterLevel = 1,
                },
                new()
                {
                    nodeId = "granite_guard",
                    displayName = "Granite Guard",
                    parentNodeId = "mountain_fist",
                    requiredCharacterLevel = 1,
                    requiredClanMemberRank = 1,
                },
                new()
                {
                    nodeId = "stone_whisper",
                    displayName = "Stone Whisper",
                    parentNodeId = "earth_sight",
                    requiredCharacterLevel = 1,
                    requiredClanMemberRank = 1,
                },
            };
            _assets.Add(graph);
            return graph;
        }
    }
}
