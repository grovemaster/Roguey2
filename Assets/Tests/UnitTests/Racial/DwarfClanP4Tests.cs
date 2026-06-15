using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Item.Essence;
using JRogue.Manager.Party;
using JRogue.Quest;
using JRogue.Racial;
using JRogue.Shop;
using JRogue.Stats;
using JRogue.Stats.Racial;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.Racial
{
    [TestFixture]
    public sealed class DwarfClanP4Tests
    {
        readonly List<GameObject> _created = new();
        readonly List<Object> _assets = new();

        [SetUp]
        public void SetUp()
        {
            var ledgerGo = new GameObject("DwarfClanP4Ledger");
            _created.Add(ledgerGo);
            ledgerGo.AddComponent<PartyCurrencyLedger>();
        }

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

            if (DwarfClanWorldState.Instance != null)
                Object.DestroyImmediate(DwarfClanWorldState.Instance.gameObject);
        }

        [Test]
        public void AddPrestige_IncrementsWorldState()
        {
            DwarfClanDefinition clan = CreateClan(DwarfClanIds.ForgeBrothersClanId);
            DwarfClanWorldState world = DwarfClanWorldState.EnsureInstance();
            world.EnsurePrestige(clan.clanId, clan.startingPrestige);

            int total = DwarfClanPrestigeService.AddPrestige(clan, 5, "test");

            Assert.AreEqual(10, total);
            Assert.AreEqual(10, world.GetPrestige(clan.clanId));
        }

        [Test]
        public void TryResolveDonationTier_LargeTierGrantsThreePrestige()
        {
            Assert.IsTrue(
                DwarfClanDonationLogic.TryResolveDonationTier(
                    DwarfClanDonationLogic.LargeDonationGold,
                    out int prestigeGained,
                    out string error),
                error);
            Assert.AreEqual(DwarfClanDonationLogic.LargeDonationPrestige, prestigeGained);
        }

        [Test]
        public void TryDonateGold_DeniesOutsideSafeZone()
        {
            DwarfClanDefinition clan = CreateClan(DwarfClanIds.ForgeBrothersClanId);
            ShopGoldUtility.AddPartyGold(100);

            Assert.IsFalse(DwarfClanPrestigeService.TryDonateGold(
                clan,
                DwarfClanDonationLogic.LargeDonationGold,
                out _,
                out _,
                out string error));
            Assert.That(error, Does.Contain("town").Or.Contain("safe").IgnoreCase);
        }

        [Test]
        public void ResolveDevotionQuestId_MapsForgeBrothers()
        {
            DwarfClanDefinition clan = CreateClan(DwarfClanIds.ForgeBrothersClanId);

            Assert.AreEqual(
                DwarfClanIds.ForgeBrothersDevotionQuestId,
                DwarfClanQuestLogic.ResolveDevotionQuestId(clan));
        }

        [Test]
        public void GetFrontierOffers_ShowsDisabledNodeWhenPrestigeTooLow()
        {
            DwarfClanDefinition clan = CreateClanWithPrestigeGate();
            BaseActor dwarf = CreateMember(clan);
            DwarfClanJoinService.ApplyJoinClan(dwarf, clan, out _);
            DwarfAncestorLearnService.ApplyLearnNode(dwarf, clan, "forge_blessing", out _);

            List<DwarfAncestorFrontierOffer> offers =
                DwarfAncestorLearnLogic.GetFrontierOffers(dwarf, clan);

            Assert.AreEqual(1, offers.Count);
            Assert.AreEqual("stone_endurance", offers[0].Node.nodeId);
            Assert.IsFalse(offers[0].Selectable);
            Assert.That(offers[0].DisabledReason, Does.Contain("prestige 10"));
        }

        [Test]
        public void ApplyLearnNode_SucceedsAfterPrestigeRaised()
        {
            DwarfClanDefinition clan = CreateClanWithPrestigeGate();
            BaseActor dwarf = CreateMember(clan);
            DwarfClanJoinService.ApplyJoinClan(dwarf, clan, out _);
            DwarfAncestorLearnService.ApplyLearnNode(dwarf, clan, "forge_blessing", out _);
            DwarfClanPrestigeService.AddPrestige(clan, 5, "test");

            Assert.IsTrue(
                DwarfAncestorLearnService.ApplyLearnNode(dwarf, clan, "stone_endurance", out string error),
                error);
        }

        [Test]
        public void TryApplyQuestReward_RaisesClanPrestige_ForPackClan()
        {
            DwarfClanWorldState.EnsureInstance()
                .EnsurePrestige(DwarfClanIds.ForgeBrothersClanId, 5);

            var rewards = new QuestRewardBundle
            {
                clanPrestige = 5,
                clanPrestigeClanId = DwarfClanIds.ForgeBrothersClanId,
            };

            Assert.IsTrue(DwarfClanPrestigeService.TryApplyQuestReward(rewards, out string error), error);
            Assert.AreEqual(10, DwarfClanWorldState.Instance.GetPrestige(DwarfClanIds.ForgeBrothersClanId));
        }

        BaseActor CreateMember(DwarfClanDefinition clan)
        {
            GameObject go = new GameObject("DwarfP4Test");
            _created.Add(go);
            var actor = go.AddComponent<BaseActor>();
            var stats = go.AddComponent<CharacterStats>();
            stats.race = Race.Dwarf;
            stats.racialSubsystem = RacialSubsystemKind.DwarfAncestry;
            stats.level = 10;
            go.AddComponent<DwarfAncestorPathRuntime>();
            go.AddComponent<DwarfClanMembershipRuntime>();
            return actor;
        }

        DwarfClanDefinition CreateClan(string clanId)
        {
            var clan = ScriptableObject.CreateInstance<DwarfClanDefinition>();
            clan.clanId = clanId;
            clan.displayName = clanId;
            clan.shortName = clanId;
            clan.startingPrestige = 5;
            clan.patronAncestor = CreatePatron(CreateLinearTree());
            _assets.Add(clan);
            return clan;
        }

        DwarfClanDefinition CreateClanWithPrestigeGate()
        {
            var clan = CreateClan(DwarfClanIds.ForgeBrothersClanId);
            clan.patronAncestor = CreatePatron(CreateTreeWithPrestigeGate());
            return clan;
        }

        AncestorDefinition CreatePatron(SpiritImprintGraph tree)
        {
            var patron = ScriptableObject.CreateInstance<AncestorDefinition>();
            patron.ancestorId = "patron";
            patron.abilityTree = tree;
            _assets.Add(patron);
            return patron;
        }

        SpiritImprintGraph CreateLinearTree()
        {
            var graph = ScriptableObject.CreateInstance<SpiritImprintGraph>();
            graph.rootNodeId = "ancestor_root";
            graph.nodes = new List<SpiritImprintNodeData> { new() { nodeId = "ancestor_root" } };
            _assets.Add(graph);
            return graph;
        }

        SpiritImprintGraph CreateTreeWithPrestigeGate()
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
                new()
                {
                    nodeId = "stone_endurance",
                    parentNodeId = "forge_blessing",
                    requiredCharacterLevel = 1,
                    requiredClanMemberRank = 1,
                    requiredClanPrestige = 10,
                },
            };
            _assets.Add(graph);
            return graph;
        }
    }
}
