using System.Collections.Generic;
using JRogue.Data.Enemy;
using JRogue.Data.Item;
using JRogue.Item;
using JRogue.Manager.Floor;
using JRogue.Manager.Loot;
using JRogue.Manager.Party;
using JRogue.Service.Loot;
using JRogue.Tests.UnitTests.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace JRogue.Tests.UnitTests.Loot
{
    [TestFixture]
    public class EnemyLootRollerTests
    {
        readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            foreach (Object obj in _created)
            {
                if (obj != null)
                    Object.DestroyImmediate(obj);
            }

            _created.Clear();
            InputTestSceneBuilder.ResetSingletonManagersForTests();
        }

        [Test]
        public void SkeletonLoot_AlwaysDropsAtLeastOneTier9()
        {
            ManaStoneTierCatalog catalog = CreateCatalog();
            EnemyLootTable table = CreateSkeletonTable();
            var rng = new QueueLootRandom(0f, 1f);

            List<ItemInstance> drops = EnemyLootRoller.Roll(table, "skeleton", catalog, rng);

            Assert.GreaterOrEqual(drops.Count, 1);
            Assert.AreEqual(9, ((ManaStoneItemData)drops[0].Definition).tier);
            Assert.AreEqual("skeleton", drops[0].ManaStoneSourceSpeciesId);
        }

        [Test]
        public void SkeletonLoot_SecondRollAtFiftyPercent()
        {
            ManaStoneTierCatalog catalog = CreateCatalog();
            EnemyLootTable table = CreateSkeletonTable();

            List<ItemInstance> two = EnemyLootRoller.Roll(
                table, "skeleton", catalog, new QueueLootRandom(0f, 0.49f));
            List<ItemInstance> one = EnemyLootRoller.Roll(
                table, "skeleton", catalog, new QueueLootRandom(0f, 0.51f));

            Assert.AreEqual(2, two.Count);
            Assert.AreEqual(1, one.Count);
        }

        [Test]
        public void GiantSkeletonLoot_AlwaysDropsThreeTier8()
        {
            ManaStoneTierCatalog catalog = CreateCatalog();
            EnemyLootTable table = CreateGiantSkeletonTable();
            var rng = new QueueLootRandom(0f, 0f, 0f, 1f);

            List<ItemInstance> drops = EnemyLootRoller.Roll(table, "giant_skeleton", catalog, rng);

            Assert.AreEqual(3, drops.Count);
            foreach (ItemInstance drop in drops)
            {
                Assert.AreEqual(8, ((ManaStoneItemData)drop.Definition).tier);
                Assert.AreEqual("giant_skeleton", drop.ManaStoneSourceSpeciesId);
            }
        }

        [Test]
        public void GiantSkeletonLoot_OptionalFourthAtThirtyPercent()
        {
            ManaStoneTierCatalog catalog = CreateCatalog();
            EnemyLootTable table = CreateGiantSkeletonTable();

            List<ItemInstance> four = EnemyLootRoller.Roll(
                table, "giant_skeleton", catalog, new QueueLootRandom(0f, 0f, 0f, 0.29f));
            List<ItemInstance> three = EnemyLootRoller.Roll(
                table, "giant_skeleton", catalog, new QueueLootRandom(0f, 0f, 0f, 0.31f));

            Assert.AreEqual(4, four.Count);
            Assert.AreEqual(3, three.Count);
        }

        [Test]
        public void ManaStoneAutoPickup_AddsToLedgerAndClearsTile()
        {
            LogAssert.ignoreFailingMessages = true;
            InputTestSceneBuilder.ResetSingletonManagersForTests();

            var pileGo = new GameObject("FloorItemPileService");
            _created.Add(pileGo);
            pileGo.AddComponent<FloorItemPileService>();

            var ledgerGo = new GameObject("PartyManaStoneLedger");
            _created.Add(ledgerGo);
            ledgerGo.AddComponent<PartyManaStoneLedger>();

            var pickupGo = new GameObject("ManaStoneAutoPickupService");
            _created.Add(pickupGo);
            pickupGo.AddComponent<ManaStoneAutoPickupService>();

            ManaStoneItemData tier9 = CreateTier(9);
            Vector3Int tile = new Vector3Int(2, 3, 0);
            FloorItemPileService.Instance.AddEntry(tile, ItemInstance.CreateManaStone(tier9, "skeleton"));

            Assert.AreEqual(1, FloorItemPileService.Instance.CountManaStonesAt(tile));

            ManaStoneAutoPickupService.Instance.TryAutoPickupManaStonesAt(tile);

            Assert.AreEqual(0, FloorItemPileService.Instance.CountManaStonesAt(tile));
            Assert.AreEqual(1, PartyManaStoneLedger.Instance.GetAmount(9, "skeleton"));
        }

        ManaStoneTierCatalog CreateCatalog()
        {
            var catalog = ScriptableObject.CreateInstance<ManaStoneTierCatalog>();
            _created.Add(catalog);

            var tiers = new ManaStoneItemData[10];
            tiers[8] = CreateTier(8);
            tiers[9] = CreateTier(9);

            typeof(ManaStoneTierCatalog)
                .GetField("tiersByNumber", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(catalog, tiers);

            return catalog;
        }

        ManaStoneItemData CreateTier(int tier)
        {
            var def = ScriptableObject.CreateInstance<ManaStoneItemData>();
            def.tier = tier;
            def.itemName = $"Mana Stone (Tier {tier})";
            def.category = ItemCategory.Currency;
            def.weight = 0f;
            def.autoPickupOnStep = true;
            _created.Add(def);
            return def;
        }

        static EnemyLootTable CreateSkeletonTable()
        {
            var table = ScriptableObject.CreateInstance<EnemyLootTable>();
            table.entries = new List<LootTableEntry>
            {
                new LootTableEntry { dropChance = 1f, payload = LootTablePayload.ManaStone, manaStoneTier = 9 },
                new LootTableEntry { dropChance = 0.5f, payload = LootTablePayload.ManaStone, manaStoneTier = 9 }
            };
            return table;
        }

        static EnemyLootTable CreateGiantSkeletonTable()
        {
            var table = ScriptableObject.CreateInstance<EnemyLootTable>();
            table.entries = new List<LootTableEntry>
            {
                new LootTableEntry { dropChance = 1f, payload = LootTablePayload.ManaStone, manaStoneTier = 8 },
                new LootTableEntry { dropChance = 1f, payload = LootTablePayload.ManaStone, manaStoneTier = 8 },
                new LootTableEntry { dropChance = 1f, payload = LootTablePayload.ManaStone, manaStoneTier = 8 },
                new LootTableEntry { dropChance = 0.3f, payload = LootTablePayload.ManaStone, manaStoneTier = 8 }
            };
            return table;
        }

        sealed class QueueLootRandom : ILootRandom
        {
            readonly Queue<float> _values;

            public QueueLootRandom(params float[] values) => _values = new Queue<float>(values);

            public float NextFloat() => _values.Count > 0 ? _values.Dequeue() : 0f;
        }
    }
}
