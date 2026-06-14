using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Item.Essence;
using JRogue.Racial;
using JRogue.Stats;
using JRogue.Stats.Racial;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.Racial
{
    [TestFixture]
    public sealed class DwarfClanSystemTests
    {
        readonly List<GameObject> _created = new List<GameObject>();
        readonly List<Object> _assets = new List<Object>();

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
        public void TryJoinClan_SetsMembershipAndRootPath()
        {
            DwarfClanDefinition clan = CreateClan();
            BaseActor dwarf = CreateUnaffiliatedDwarf();

            Assert.IsTrue(DwarfClanJoinService.ApplyJoinClan(dwarf, clan, out string error), error);

            DwarfClanMembershipRuntime membership = dwarf.GetComponent<DwarfClanMembershipRuntime>();
            DwarfAncestorPathRuntime path = dwarf.GetComponent<DwarfAncestorPathRuntime>();

            Assert.IsTrue(membership.IsAffiliated);
            Assert.AreEqual(DwarfClanIds.ForgeBrothersClanId, membership.ClanId);
            Assert.AreEqual(0, membership.ClanMemberRank);
            Assert.AreEqual(clan.patronAncestor, path.PatronAncestor);
            Assert.AreEqual(1, path.ChosenPathNodeIds.Count);
            Assert.AreEqual("ancestor_root", path.ChosenPathNodeIds[0]);
        }

        [Test]
        public void GetFrontierOffers_ReturnsChildAfterJoin()
        {
            DwarfClanDefinition clan = CreateClan();
            BaseActor dwarf = CreateUnaffiliatedDwarf();
            DwarfClanJoinService.ApplyJoinClan(dwarf, clan, out _);

            List<DwarfAncestorFrontierOffer> offers = DwarfAncestorLearnLogic.GetFrontierOffers(dwarf, clan);

            Assert.AreEqual(1, offers.Count);
            Assert.AreEqual("forge_blessing", offers[0].Node.nodeId);
            Assert.IsTrue(offers[0].Selectable);
        }

        [Test]
        public void TryLearnNode_AppendsTechniqueAndIncrementsRank()
        {
            DwarfClanDefinition clan = CreateClan();
            BaseActor dwarf = CreateUnaffiliatedDwarf();
            DwarfClanJoinService.ApplyJoinClan(dwarf, clan, out _);

            Assert.IsTrue(
                DwarfAncestorLearnService.ApplyLearnNode(dwarf, clan, "forge_blessing", out string error),
                error);

            DwarfClanMembershipRuntime membership = dwarf.GetComponent<DwarfClanMembershipRuntime>();
            DwarfAncestorPathRuntime path = dwarf.GetComponent<DwarfAncestorPathRuntime>();

            Assert.AreEqual(1, membership.ClanMemberRank);
            Assert.IsTrue(path.IsNodeLearned("forge_blessing"));
        }

        [Test]
        public void NonDwarf_CannotJoin()
        {
            DwarfClanDefinition clan = CreateClan();
            GameObject go = new GameObject("Human");
            _created.Add(go);
            var human = go.AddComponent<BaseActor>();
            var stats = go.AddComponent<CharacterStats>();
            stats.race = Race.Human;

            Assert.IsFalse(DwarfClanJoinService.ApplyJoinClan(human, clan, out string error));
            Assert.That(error, Does.Contain("dwarf"));
        }

        BaseActor CreateUnaffiliatedDwarf()
        {
            GameObject go = new GameObject("DwarfTest");
            _created.Add(go);
            var actor = go.AddComponent<BaseActor>();
            var stats = go.AddComponent<CharacterStats>();
            stats.race = Race.Dwarf;
            stats.racialSubsystem = RacialSubsystemKind.DwarfAncestry;
            stats.level = 5;
            go.AddComponent<DwarfAncestorPathRuntime>();
            return actor;
        }

        DwarfClanDefinition CreateClan()
        {
            var clan = ScriptableObject.CreateInstance<DwarfClanDefinition>();
            clan.clanId = DwarfClanIds.ForgeBrothersClanId;
            clan.displayName = "Forge Brothers";
            clan.startingPrestige = 5;
            clan.patronAncestor = CreatePatron();
            _assets.Add(clan);
            return clan;
        }

        AncestorDefinition CreatePatron()
        {
            var patron = ScriptableObject.CreateInstance<AncestorDefinition>();
            patron.ancestorId = "forge_father";
            patron.abilityTree = CreateTree();
            _assets.Add(patron);
            return patron;
        }

        SpiritImprintGraph CreateTree()
        {
            var graph = ScriptableObject.CreateInstance<SpiritImprintGraph>();
            graph.rootNodeId = "ancestor_root";

            var root = new SpiritImprintNodeData { nodeId = "ancestor_root" };
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
    }
}
