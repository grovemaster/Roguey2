using System.Collections.Generic;
using JRogue.Ability.Telekinesis;
using JRogue.Actors;
using JRogue.Item;
using JRogue.Item.Essence;
using JRogue.Item.World;
using JRogue.Manager.Essence;
using JRogue.Manager.Floor;
using JRogue.Manager.Inventory;
using JRogue.Manager.Party;
using JRogue.Stats;
using JRogue.Tests.UnitTests.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace JRogue.Tests.UnitTests.Essence
{
    [TestFixture]
    public sealed class TelekinesisAbilityTests
    {
        readonly List<GameObject> _created = new List<GameObject>();
        readonly List<Object> _assets = new List<Object>();

        [SetUp]
        public void SetUp() => InputTestSceneBuilder.ResetSingletonManagersForTests();

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

            InputTestSceneBuilder.ResetSingletonManagersForTests();
        }

        [Test]
        public void ValidPull_AddsItemAndDeductsSoulPower()
        {
            SetupActorWithTelekinesis(out BaseActor actor, out EssenceSlotManager essence, out _);
            Vector3Int itemTile = actor.GridPosition + Vector3Int.right;
            ItemData sword = CreateTreasure("Test Sword", weight: 1f);
            FloorItemPileService.Instance.AddEntry(itemTile, new ItemInstance(sword));

            int spBefore = actor.stats.currentSoulPower;
            Assert.IsTrue(essence.TryExecuteAbility(0, 0, itemTile));
            Assert.AreEqual(spBefore - 1, actor.stats.currentSoulPower);
            Assert.AreEqual(1, actor.GetComponent<InventoryManager>().CarriedItems.Count);
        }

        [Test]
        public void Encumbered_PlacesItemOnPlayerTile()
        {
            SetupActorWithTelekinesis(out BaseActor actor, out EssenceSlotManager essence, out _);
            actor.stats.Constitution = new Stat(1);
            Vector3Int itemTile = actor.GridPosition + Vector3Int.up;
            ItemData heavy = CreateTreasure("Anvil", weight: 500f);
            FloorItemPileService.Instance.AddEntry(itemTile, new ItemInstance(heavy));

            Vector3Int feet = actor.GridPosition;
            Assert.IsTrue(essence.TryExecuteAbility(0, 0, itemTile));
            Assert.AreEqual(0, actor.GetComponent<InventoryManager>().CarriedItems.Count);
            Assert.AreEqual(1, FloorItemPileService.Instance.GetEntries(feet).Count);
        }

        [Test]
        public void InvalidEmptyTile_ExecuteFalse_NoSoulPowerDeduct()
        {
            SetupActorWithTelekinesis(out BaseActor actor, out EssenceSlotManager essence, out _);
            Vector3Int empty = actor.GridPosition + Vector3Int.left;
            int spBefore = actor.stats.currentSoulPower;

            LogAssert.Expect(LogType.Log, $"[Telekinesis] Invalid target at tile ({empty.x}, {empty.y}, {empty.z}).");

            Assert.IsFalse(essence.TryExecuteAbility(0, 0, empty));
            Assert.AreEqual(spBefore, actor.stats.currentSoulPower);
        }

        [Test]
        public void InvalidMultiItemTile_ExecuteFalse()
        {
            SetupActorWithTelekinesis(out BaseActor actor, out EssenceSlotManager essence, out _);
            Vector3Int tile = actor.GridPosition + new Vector3Int(2, 0, 0);
            FloorItemPileService.Instance.AddEntry(tile, new ItemInstance(CreateTreasure("A", 1f)));
            FloorItemPileService.Instance.AddEntry(tile, new ItemInstance(CreateTreasure("B", 1f)));

            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex(@"\[Telekinesis\] Invalid target"));

            Assert.IsFalse(essence.TryExecuteAbility(0, 0, tile));
        }

        [Test]
        public void RangeExceeded_IsInvalid()
        {
            SetupActorWithTelekinesis(out BaseActor actor, out EssenceSlotManager essence, out TelekinesisAbility ability);
            ability.range = 3;
            Vector3Int far = actor.GridPosition + new Vector3Int(4, 0, 0);
            ItemData item = CreateTreasure("Far Item", 1f);
            FloorItemPileService.Instance.AddEntry(far, new ItemInstance(item));

            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex(@"\[Telekinesis\] Invalid target"));

            Assert.IsFalse(essence.TryExecuteAbility(0, 0, far));
        }

        [Test]
        public void TryGetSinglePickable_RejectsCurrencyOnPile()
        {
            EnsurePileService();
            Vector3Int tile = new Vector3Int(5, 5, 0);
            var coin = ScriptableObject.CreateInstance<ItemData>();
            coin.itemName = "Gold";
            coin.category = ItemCategory.Currency;
            _assets.Add(coin);
            FloorItemPileService.Instance.AddEntry(tile, new ItemInstance(coin, qty: 10));

            Assert.IsFalse(TelekinesisFloorQuery.TryGetSinglePickable(tile, out _));
        }

        [Test]
        public void TryGetSinglePickable_RejectsPilePlusWorldItemCoexistence()
        {
            EnsurePileService();
            Vector3Int tile = new Vector3Int(6, 6, 0);
            FloorItemPileService.Instance.AddEntry(tile, new ItemInstance(CreateTreasure("Pile Gem", 1f)));

            var worldGo = new GameObject("WorldSword");
            _created.Add(worldGo);
            worldGo.transform.position = FloorItemPileService.TileCenterWorld(tile);
            var worldItem = worldGo.AddComponent<WorldItem>();
            worldItem.data = CreateTreasure("World Sword", 1f);

            Assert.IsFalse(TelekinesisFloorQuery.TryGetSinglePickable(tile, out _));
        }

        void SetupActorWithTelekinesis(
            out BaseActor actor,
            out EssenceSlotManager essence,
            out TelekinesisAbility ability)
        {
            InputTestSceneBuilder.SetupMapAndManagers(_created);
            EnsurePileService();

            PartyManager party = InputTestSceneBuilder.CreatePartyWithTestActors(1, _created);
            actor = party.partyMembers[0];
            essence = actor.GetComponent<EssenceSlotManager>();
            actor.SetGridPosition(Vector3Int.zero);
            InputTestSceneBuilder.RegisterCurrentPartyOnGrid(party.partyMembers);

            ability = ScriptableObject.CreateInstance<TelekinesisAbility>();
            ability.requiresTarget = true;
            ability.soulPowerCost = 1;
            ability.range = 7;
            _assets.Add(ability);

            var essenceData = ScriptableObject.CreateInstance<EssenceData>();
            essenceData.statModifiers = new List<AttributeModifier>();
            essenceData.resistanceModifiers = new List<DamageResistanceModifier>();
            essenceData.complexPassives = new List<PassiveEffect>();
            essenceData.activeAbilities = new List<JRogue.Ability.AbilityAction> { ability };
            _assets.Add(essenceData);

            essence.EquipEssence(essenceData, 0);
            actor.stats.currentSoulPower = 10;
            actor.SetGridPosition(Vector3Int.zero);
        }

        void EnsurePileService()
        {
            if (FloorItemPileService.Instance != null)
                return;

            var pileGo = new GameObject("FloorItemPileService");
            _created.Add(pileGo);
            pileGo.AddComponent<FloorItemPileService>();
        }

        ItemData CreateTreasure(string name, float weight)
        {
            var def = ScriptableObject.CreateInstance<ItemData>();
            def.itemName = name;
            def.category = ItemCategory.Treasure;
            def.weight = weight;
            _assets.Add(def);
            return def;
        }
    }
}
