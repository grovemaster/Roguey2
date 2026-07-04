using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Actors.Components;
using JRogue.Item;
using JRogue.Manager.Inventory;
using JRogue.Manager.Party;
using JRogue.Stats;
using JRogue.UI.Hotbar;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.Inventory
{
    [TestFixture]
    public sealed class PartyInventoryTransferServiceTests
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

            if (PartyManager.Instance != null)
                Object.DestroyImmediate(PartyManager.Instance.gameObject);
        }

        [Test]
        public void TryGiveCarriedItem_partial_reduces_source()
        {
            (BaseActor giver, BaseActor recipient, ItemInstance stack) = CreateTransferPair();
            stack.Quantity = 5;

            Assert.IsTrue(PartyInventoryTransferService.TryGiveCarriedItem(
                stack, giver, recipient, 2, out string message));

            Assert.AreEqual(3, stack.Quantity);
            Assert.AreEqual(1, giver.GetComponent<InventoryManager>().CarriedItems.Count);
            Assert.AreEqual(1, recipient.GetComponent<InventoryManager>().CarriedItems.Count);
            Assert.AreEqual(2, recipient.GetComponent<InventoryManager>().CarriedItems[0].Quantity);
            Assert.IsTrue(message.Contains("2"));
        }

        [Test]
        public void TryGiveCarriedItem_partial_merges_recipient()
        {
            (BaseActor giver, BaseActor recipient, ItemInstance stack) = CreateTransferPair();
            stack.Quantity = 2;

            InventoryManager recipientInventory = recipient.GetComponent<InventoryManager>();
            var existing = new ItemInstance(stack.Definition, 3);
            recipientInventory.AddItem(existing);

            Assert.IsTrue(PartyInventoryTransferService.TryGiveCarriedItem(
                stack, giver, recipient, 2, out _));

            Assert.AreEqual(1, recipientInventory.CarriedItems.Count);
            Assert.AreEqual(5, recipientInventory.CarriedItems[0].Quantity);
            Assert.AreEqual(existing.Id, recipientInventory.CarriedItems[0].Id);
        }

        [Test]
        public void TryGiveCarriedItem_full_removes_source_instance()
        {
            (BaseActor giver, BaseActor recipient, ItemInstance stack) = CreateTransferPair();
            stack.Quantity = 5;
            string stackId = stack.Id;

            Assert.IsTrue(PartyInventoryTransferService.TryGiveCarriedItem(
                stack, giver, recipient, 5, out _));

            Assert.AreEqual(0, giver.GetComponent<InventoryManager>().CarriedItems.Count);
            Assert.AreEqual(1, recipient.GetComponent<InventoryManager>().CarriedItems.Count);
            Assert.AreEqual(stackId, recipient.GetComponent<InventoryManager>().CarriedItems[0].Id);
            Assert.AreEqual(5, recipient.GetComponent<InventoryManager>().CarriedItems[0].Quantity);
        }

        [Test]
        public void TryGiveCarriedItem_over_quantity_fails()
        {
            (BaseActor giver, BaseActor recipient, ItemInstance stack) = CreateTransferPair();
            stack.Quantity = 5;

            Assert.IsFalse(PartyInventoryTransferService.TryGiveCarriedItem(
                stack, giver, recipient, 6, out string message));

            Assert.AreEqual(5, stack.Quantity);
            Assert.IsTrue(message.Contains("Not enough"));
        }

        [Test]
        public void TryGiveCarriedItem_encumbrance_partial_fails_without_mutation()
        {
            (BaseActor giver, BaseActor recipient, ItemInstance stack) = CreateTransferPair();
            stack.Quantity = 5;
            stack.Definition.weight = 2f;

            CharacterStats recipientStats = recipient.GetComponent<CharacterStats>();
            recipientStats.Constitution = new Stat(1);

            Assert.IsFalse(PartyInventoryTransferService.TryGiveCarriedItem(
                stack, giver, recipient, 5, out _));
            Assert.AreEqual(5, stack.Quantity);
            Assert.AreEqual(1, giver.GetComponent<InventoryManager>().CarriedItems.Count);
            Assert.AreEqual(0, recipient.GetComponent<InventoryManager>().CarriedItems.Count);

            Assert.IsTrue(PartyInventoryTransferService.TryGiveCarriedItem(
                stack, giver, recipient, 2, out _));
            Assert.AreEqual(3, stack.Quantity);
            Assert.AreEqual(1, recipient.GetComponent<InventoryManager>().CarriedItems.Count);
            Assert.AreEqual(2, recipient.GetComponent<InventoryManager>().CarriedItems[0].Quantity);
        }

        [Test]
        public void TryGiveCarriedItem_full_clears_hotbar()
        {
            (BaseActor giver, BaseActor recipient, ItemInstance stack) = CreateTransferPair();
            HotbarLayout layout = giver.gameObject.AddComponent<HotbarLayout>();
            layout.SetSlot(0, new HotbarEntry
            {
                Kind = HotbarEntryKind.InventoryUse,
                itemInstanceId = stack.Id,
            });

            Assert.IsTrue(PartyInventoryTransferService.TryGiveCarriedItem(
                stack, giver, recipient, 1, out _));

            Assert.IsTrue(layout.GetSlot(0).IsEmpty());
        }

        [Test]
        public void TryGiveCarriedItem_partial_keeps_hotbar()
        {
            (BaseActor giver, BaseActor recipient, ItemInstance stack) = CreateTransferPair();
            stack.Quantity = 5;
            HotbarLayout layout = giver.gameObject.AddComponent<HotbarLayout>();
            layout.SetSlot(0, new HotbarEntry
            {
                Kind = HotbarEntryKind.InventoryUse,
                itemInstanceId = stack.Id,
            });

            Assert.IsTrue(PartyInventoryTransferService.TryGiveCarriedItem(
                stack, giver, recipient, 2, out _));

            Assert.AreEqual(stack.Id, layout.GetSlot(0).itemInstanceId);
            Assert.AreEqual(3, stack.Quantity);
        }

        (BaseActor giver, BaseActor recipient, ItemInstance stack) CreateTransferPair()
        {
            PartyManager party = CreatePartyManager();
            BaseActor giver = CreatePartyActor("Giver");
            BaseActor recipient = CreatePartyActor("Recipient");
            party.partyMembers.Add(giver);
            party.partyMembers.Add(recipient);

            ItemData potion = CreatePotion();
            var stack = new ItemInstance(potion, 1);
            giver.GetComponent<InventoryManager>().AddItem(stack);

            return (giver, recipient, stack);
        }

        static PartyManager CreatePartyManager()
        {
            var go = new GameObject("PartyManager");
            Object.DontDestroyOnLoad(go);
            PartyManager pm = go.AddComponent<PartyManager>();
            pm.partyMembers = new List<BaseActor>();
            return pm;
        }

        BaseActor CreatePartyActor(string name)
        {
            GameObject go = new GameObject(name);
            _created.Add(go);
            var stats = go.AddComponent<CharacterStats>();
            stats.Constitution = new Stat(20);
            go.AddComponent<HealthComponent>();
            go.AddComponent<InventoryManager>();
            return go.AddComponent<TestPartyActor>();
        }

        ItemData CreatePotion()
        {
            var potion = ScriptableObject.CreateInstance<ItemData>();
            _assets.Add(potion);
            potion.itemName = "Healing Potion";
            potion.category = ItemCategory.Potion;
            potion.weight = 0.2f;
            return potion;
        }

        sealed class TestPartyActor : BaseActor
        {
            protected override void Die() { }
        }
    }
}
