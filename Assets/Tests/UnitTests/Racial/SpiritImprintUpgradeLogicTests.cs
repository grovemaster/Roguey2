using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Dialog;
using JRogue.Item;
using JRogue.Item.Essence;
using JRogue.Manager.Inventory;
using JRogue.Manager.Party;
using JRogue.Quest;
using JRogue.Racial;
using JRogue.Shop;
using JRogue.Stats;
using JRogue.Stats.Racial;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.Racial
{
    public sealed class SpiritImprintUpgradeLogicTests
    {
        sealed class TestPartyActor : BaseActor
        {
            protected override void Die()
            {
            }
        }

        readonly List<Object> _assets = new List<Object>();
        readonly List<GameObject> _objects = new List<GameObject>();
        GameStoryFlagService _flags;
        PartyCurrencyLedger _ledger;

        [SetUp]
        public void SetUp()
        {
            var flagGo = new GameObject("Flags");
            _objects.Add(flagGo);
            _flags = flagGo.AddComponent<GameStoryFlagService>();
            _flags.ClearAll();

            var ledgerGo = new GameObject("Ledger");
            _objects.Add(ledgerGo);
            _ledger = ledgerGo.AddComponent<PartyCurrencyLedger>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object asset in _assets)
            {
                if (asset != null)
                    Object.DestroyImmediate(asset);
            }

            _assets.Clear();

            foreach (GameObject go in _objects)
            {
                if (go != null)
                    Object.DestroyImmediate(go);
            }

            _objects.Clear();
        }

        [Test]
        public void IsSpeakerEligible_RejectsNonBarbarian()
        {
            TestPartyActor actor = CreateBarbarianActor(withImprint: false);
            actor.GetComponent<CharacterStats>().race = Race.Human;

            bool ok = SpiritImprintUpgradeLogic.IsSpeakerEligible(actor, out _, out string line);
            Assert.IsFalse(ok);
            Assert.AreEqual("Hello. You are not a Barbarian.", line);
        }

        [Test]
        public void CanAfford_RequiresGoldItemsAndFlags()
        {
            ItemData blade = CreateItem("Giants_Blade");
            TestPartyActor speaker = CreateBarbarianActor(withImprint: true);
            AddCarriedItem(speaker, blade, 1);
            GrantGold(100);

            var cost = new SpiritImprintUnlockCost
            {
                gold = 30,
                items = new[] { new SpiritImprintItemCost { item = blade, quantity = 1 } },
                storyFlags = new[] { new SpiritImprintFlagCost { flagId = "quest_skeleton_proof", expectedValue = true } },
            };

            var party = new List<BaseActor> { speaker };
            Assert.IsFalse(SpiritImprintUpgradeLogic.CanAfford(
                cost,
                SpiritImprintUpgradeLogic.OrderPartyMembersForPayment(party, speaker),
                _flags,
                out _));

            _flags.Set("quest_skeleton_proof");
            Assert.IsTrue(SpiritImprintUpgradeLogic.CanAfford(
                cost,
                SpiritImprintUpgradeLogic.OrderPartyMembersForPayment(party, speaker),
                _flags,
                out _));
        }

        [Test]
        public void TryExecuteUpgrade_AppendsNodeAndSpendsGold()
        {
            SpiritImprintGraph graph = BuildSampleGraph();
            TestPartyActor speaker = CreateBarbarianActor(withImprint: true, graph);
            GrantGold(50);

            graph.nodes[1].unlockCost = new SpiritImprintUnlockCost { gold = 30 };

            bool ok = SpiritImprintUpgradeLogic.TryExecuteUpgrade(
                speaker,
                speaker.GetComponent<SpiritImprintRuntime>(),
                "tier1_str",
                new List<BaseActor> { speaker },
                _flags,
                out string failure);

            Assert.IsTrue(ok, failure);
            Assert.AreEqual(2, speaker.GetComponent<SpiritImprintRuntime>().ChosenPathNodeIds.Count);
            Assert.AreEqual("tier1_str", speaker.GetComponent<SpiritImprintRuntime>().ChosenPathNodeIds[1]);
            Assert.AreEqual(20, ShopGoldUtility.GetPartyGoldTotal());
            Assert.AreEqual(11, speaker.GetComponent<CharacterStats>().Strength.GetValue());
        }

        [Test]
        public void GetNextNodeOffers_ReturnsDirectChildrenOfTail()
        {
            SpiritImprintGraph graph = BuildSampleGraph();
            TestPartyActor speaker = CreateBarbarianActor(withImprint: true, graph);

            IReadOnlyList<SpiritImprintNodeData> offers =
                SpiritImprintUpgradeLogic.GetNextNodeOffers(speaker.GetComponent<SpiritImprintRuntime>());
            Assert.AreEqual(2, offers.Count);
            Assert.AreEqual("tier1_str", offers[0].nodeId);
            Assert.AreEqual("tier1_dex", offers[1].nodeId);

            speaker.GetComponent<SpiritImprintRuntime>().TryAppendChild("tier1_str", out _);
            offers = SpiritImprintUpgradeLogic.GetNextNodeOffers(speaker.GetComponent<SpiritImprintRuntime>());
            Assert.AreEqual(1, offers.Count);
            Assert.AreEqual("tier2_constitution", offers[0].nodeId);
        }

        TestPartyActor CreateBarbarianActor(bool withImprint, SpiritImprintGraph graph = null)
        {
            var go = new GameObject("BarbarianSpeaker");
            _objects.Add(go);
            CharacterStats stats = go.AddComponent<CharacterStats>();
            stats.race = Race.Barbarian;
            stats.racialSubsystem = RacialSubsystemKind.SpiritImprintBarbarian;

            if (withImprint)
            {
                SpiritImprintRuntime imprint = go.AddComponent<SpiritImprintRuntime>();
                imprint.SetGraphAndChosenPath(graph ?? BuildSampleGraph(), new List<string> { "imprint_root" });
                imprint.TryApplyFromSerializedState();
            }

            return go.AddComponent<TestPartyActor>();
        }

        void GrantGold(int amount)
        {
            ItemData gold = ScriptableObject.CreateInstance<ItemData>();
            gold.itemName = "Gold";
            gold.category = ItemCategory.Currency;
            _assets.Add(gold);
            _ledger.Add(gold, amount);
        }

        void AddCarriedItem(BaseActor actor, ItemData item, int quantity)
        {
            InventoryManager inventory = actor.GetComponent<InventoryManager>();
            if (inventory == null)
                inventory = actor.gameObject.AddComponent<InventoryManager>();

            inventory.AddItem(new ItemInstance(item, quantity));
        }

        ItemData CreateItem(string name)
        {
            var item = ScriptableObject.CreateInstance<ItemData>();
            item.itemName = name;
            item.weight = 1f;
            _assets.Add(item);
            return item;
        }

        SpiritImprintGraph BuildSampleGraph()
        {
            var graph = ScriptableObject.CreateInstance<SpiritImprintGraph>();
            _assets.Add(graph);
            graph.rootNodeId = "imprint_root";
            graph.nodes = new List<SpiritImprintNodeData>
            {
                new SpiritImprintNodeData
                {
                    nodeId = "imprint_root",
                    displayName = "Root",
                    parentNodeId = "",
                    statModifiers = new List<AttributeModifier>(),
                },
                new SpiritImprintNodeData
                {
                    nodeId = "tier1_str",
                    displayName = "+1 STR",
                    parentNodeId = "imprint_root",
                    siblingExclusivityGroup = 1,
                    statModifiers = new List<AttributeModifier>
                    {
                        new AttributeModifier { attribute = StatType.Strength, value = 1 },
                    },
                },
                new SpiritImprintNodeData
                {
                    nodeId = "tier1_dex",
                    displayName = "+1 DEX",
                    parentNodeId = "imprint_root",
                    siblingExclusivityGroup = 1,
                    statModifiers = new List<AttributeModifier>
                    {
                        new AttributeModifier { attribute = StatType.Dexterity, value = 1 },
                    },
                },
                new SpiritImprintNodeData
                {
                    nodeId = "tier2_constitution",
                    displayName = "+1 CON",
                    parentNodeId = "tier1_str",
                    statModifiers = new List<AttributeModifier>
                    {
                        new AttributeModifier { attribute = StatType.Constitution, value = 1 },
                    },
                },
            };
            return graph;
        }
    }
}
