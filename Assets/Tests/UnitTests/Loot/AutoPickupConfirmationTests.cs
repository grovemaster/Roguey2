using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Input;
using JRogue.Item;
using JRogue.Item.World;
using JRogue.Manager.Floor;
using JRogue.Manager.Map;
using JRogue.Manager.Party;
using JRogue.Manager.Turn;
using JRogue.Tests.UnitTests.Input;
using JRogue.UI.Gameplay;
using JRogue.Actors.Components;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace JRogue.Tests.UnitTests.Loot
{
    [TestFixture]
    public sealed class AutoPickupConfirmationTests
    {
        readonly List<GameObject> _created = new List<GameObject>();
        readonly List<Object> _assets = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            LogAssert.ignoreFailingMessages = true;
            InputTestSceneBuilder.ResetSingletonManagersForTests();
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
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

            if (AutoPickupConfirmDialogUI.EnsureInstance() != null)
                Object.DestroyImmediate(AutoPickupConfirmDialogUI.EnsureInstance().gameObject);

            InputTestSceneBuilder.ResetSingletonManagersForTests();
        }

        [Test]
        public void FloorPile_Query_SplitsConfirmGatedAndSilent()
        {
            var pileGo = new GameObject("FloorItemPileService");
            _created.Add(pileGo);
            pileGo.AddComponent<FloorItemPileService>();

            ItemData confirm = CreateItem("Confirm Sword", confirm: true);
            ManaStoneItemData silent = CreateManaStone();

            Vector3Int tile = new Vector3Int(4, 2, 0);
            FloorItemPileService.Instance.AddEntry(tile, new ItemInstance(confirm));
            FloorItemPileService.Instance.AddEntry(tile, ItemInstance.CreateManaStone(silent, "bat"));

            Assert.AreEqual(1, FloorItemPileService.Instance.GetConfirmGatedAutoPickupEntries(tile).Count);
            Assert.AreEqual(1, FloorItemPileService.Instance.GetSilentAutoPickupEntries(tile).Count);
        }

        [Test]
        public void MoveGrid_WithConfirmGatedItem_BlocksMoveAndTurnUntilResolved()
        {
            SetupPartyWithPile(out PartyManager party, out PlayerCommandProcessor processor, out BaseActor leader);

            ItemData confirm = CreateItem("Giant's Blade", confirm: true);
            Vector3Int dest = leader.GridPosition + Vector3Int.right;
            FloorItemPileService.Instance.AddEntry(dest, new ItemInstance(confirm));

            Vector3Int start = leader.GridPosition;
            Assert.IsTrue(TurnManager.Instance.CanActorTakeAction(leader.gameObject));

            Assert.IsTrue(processor.TryApply(PlayerCommand.MoveGrid(Vector3Int.right)));
            Assert.AreEqual(start, leader.GridPosition);
            Assert.IsTrue(TurnManager.Instance.CanActorTakeAction(leader.gameObject));
            Assert.IsTrue(AutoPickupConfirmDialogUI.BlocksGameplay);
        }

        [Test]
        public void AutoPickupMoveGate_InvokesYesCallbackWhenDialogCommits()
        {
            InputTestSceneBuilder.SetupMapAndManagers(_created);

            GameObject actorGo = new GameObject("Mover");
            _created.Add(actorGo);
            actorGo.AddComponent<GridMover>();
            var actor = actorGo.AddComponent<InputTestSceneBuilder.TestPartyActor>();
            InputTestSceneBuilder.SetPrivateField(
                actor,
                "mapManager",
                MapManager.Instance);

            ItemData confirm = CreateItem("Relic", confirm: true);
            Vector3Int tile = new Vector3Int(1, 0, 0);

            var pileGo = new GameObject("FloorItemPileService");
            _created.Add(pileGo);
            pileGo.AddComponent<FloorItemPileService>();
            FloorItemPileService.Instance.AddEntry(tile, new ItemInstance(confirm));

            bool yes = false;
            Assert.IsTrue(AutoPickupMoveGate.TryInterceptMove(actor, tile, isEnemyBump: false, () => yes = true));
            Assert.IsFalse(yes);
            Assert.IsTrue(AutoPickupConfirmDialogUI.BlocksGameplay);

            var commit = typeof(AutoPickupConfirmDialogUI).GetMethod(
                "CommitYes",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            commit.Invoke(AutoPickupConfirmDialogUI.EnsureInstance(), null);
            Assert.IsTrue(yes);
        }

        void SetupPartyWithPile(
            out PartyManager party,
            out PlayerCommandProcessor processor,
            out BaseActor leader)
        {
            InputTestSceneBuilder.SetupMapAndManagers(_created);

            var pileGo = new GameObject("FloorItemPileService");
            _created.Add(pileGo);
            pileGo.AddComponent<FloorItemPileService>();

            party = InputTestSceneBuilder.CreatePartyWithTestActors(1, _created);
            leader = party.partyMembers[0];
            InputTestSceneBuilder.RegisterCurrentPartyOnGrid(party.partyMembers);
            InputTestSceneBuilder.SetPrivateField(party, "isFormationActive", false);

            processor = new PlayerCommandProcessor();
            TurnManager.Instance.currentState = GameState.PLAYER_TURN;
        }

        ItemData CreateItem(string name, bool confirm)
        {
            var def = ScriptableObject.CreateInstance<ItemData>();
            def.itemName = name;
            def.category = ItemCategory.Weapon;
            def.autoPickupOnStep = true;
            def.requiresAutoPickupConfirmation = confirm;
            _assets.Add(def);
            return def;
        }

        ManaStoneItemData CreateManaStone()
        {
            var def = ScriptableObject.CreateInstance<ManaStoneItemData>();
            def.itemName = "Mana Stone";
            def.tier = 9;
            _assets.Add(def);
            return def;
        }
    }
}
