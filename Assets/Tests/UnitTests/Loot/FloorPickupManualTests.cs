using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Input;
using JRogue.Item;
using JRogue.Manager.Floor;
using JRogue.Manager.Party;
using JRogue.Manager.Turn;
using JRogue.Tests.UnitTests.Input;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.Loot
{
    [TestFixture]
    public sealed class FloorPickupManualTests
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
        public void ManualOnlyItem_NotInAutoPickupQueries()
        {
            EnsurePileService();
            ItemData manual = CreateItem("Potion of Experience", auto: false, confirm: false);
            Vector3Int tile = new Vector3Int(3, 4, 0);
            FloorItemPileService.Instance.AddEntry(tile, new ItemInstance(manual));

            Assert.AreEqual(0, FloorItemPileService.Instance.GetSilentAutoPickupEntries(tile).Count);
            Assert.AreEqual(0, FloorItemPileService.Instance.GetConfirmGatedAutoPickupEntries(tile).Count);
            Assert.AreEqual(1, FloorItemPileService.Instance.GetEntries(tile).Count);
        }

        [Test]
        public void CollectManualTargets_IncludesPileAndCountsWorldItems()
        {
            EnsurePileService();
            Vector3Int tile = new Vector3Int(1, 1, 0);
            ItemData manual = CreateItem("Manual Gem", auto: false, confirm: false);
            FloorItemPileService.Instance.AddEntry(tile, new ItemInstance(manual));

            List<ManualPickupTarget> targets = FloorPickupCoordinator.CollectManualTargets(tile);
            Assert.AreEqual(1, targets.Count);
        }

        [Test]
        public void PickupFloorItems_WithSingleItem_ConsumesTurn()
        {
            SetupParty(out PartyManager party, out PlayerCommandProcessor processor, out BaseActor leader);

            ItemData manual = CreateItem("Lone Coin", auto: false, confirm: false);
            manual.category = ItemCategory.Currency;
            Vector3Int tile = leader.GridPosition;
            FloorItemPileService.Instance.AddEntry(tile, new ItemInstance(manual, qty: 5));

            Assert.IsTrue(TurnManager.Instance.CanActorTakeAction(leader.gameObject));
            Assert.IsTrue(processor.TryApply(PlayerCommand.PickupFloorItems()));
            Assert.IsFalse(TurnManager.Instance.CanActorTakeAction(leader.gameObject));
            Assert.AreEqual(0, FloorItemPileService.Instance.GetEntries(tile).Count);
        }

        [Test]
        public void PickupFloorItems_EmptyTile_DoesNotConsumeTurn()
        {
            SetupParty(out _, out PlayerCommandProcessor processor, out BaseActor leader);
            Assert.IsTrue(TurnManager.Instance.CanActorTakeAction(leader.gameObject));
            Assert.IsTrue(processor.TryApply(PlayerCommand.PickupFloorItems()));
            Assert.IsTrue(TurnManager.Instance.CanActorTakeAction(leader.gameObject));
        }

        [Test]
        public void AttemptPickupAllCarryable_LeavesTooHeavyOnTile()
        {
            SetupParty(out _, out _, out BaseActor leader);

            ItemData heavy = CreateItem("Boulder", auto: false, confirm: false);
            heavy.weight = 500f;
            ItemData light = CreateItem("Feather", auto: false, confirm: false);
            light.weight = 0.1f;
            Vector3Int tile = leader.GridPosition;
            FloorItemPileService.Instance.AddEntry(tile, new ItemInstance(heavy));
            FloorItemPileService.Instance.AddEntry(tile, new ItemInstance(light));

            List<ManualPickupTarget> targets = FloorPickupCoordinator.CollectManualTargets(tile);
            int picked = FloorPickupCoordinator.AttemptPickupAllCarryable(targets, leader);

            Assert.AreEqual(1, picked);
            Assert.AreEqual(1, FloorItemPileService.Instance.GetEntries(tile).Count);
        }

        void EnsurePileService()
        {
            if (FloorItemPileService.Instance != null)
                return;

            var pileGo = new GameObject("FloorItemPileService");
            _created.Add(pileGo);
            pileGo.AddComponent<FloorItemPileService>();
        }

        void SetupParty(
            out PartyManager party,
            out PlayerCommandProcessor processor,
            out BaseActor leader)
        {
            InputTestSceneBuilder.SetupMapAndManagers(_created);
            EnsurePileService();

            party = InputTestSceneBuilder.CreatePartyWithTestActors(1, _created);
            leader = party.partyMembers[0];
            InputTestSceneBuilder.RegisterCurrentPartyOnGrid(party.partyMembers);
            InputTestSceneBuilder.SetPrivateField(party, "isFormationActive", false);

            processor = new PlayerCommandProcessor();
            TurnManager.Instance.currentState = GameState.PLAYER_TURN;
        }

        ItemData CreateItem(string name, bool auto, bool confirm)
        {
            var def = ScriptableObject.CreateInstance<ItemData>();
            def.itemName = name;
            def.category = ItemCategory.Treasure;
            def.weight = 1f;
            def.autoPickupOnStep = auto;
            def.requiresAutoPickupConfirmation = confirm;
            _assets.Add(def);
            return def;
        }
    }
}
