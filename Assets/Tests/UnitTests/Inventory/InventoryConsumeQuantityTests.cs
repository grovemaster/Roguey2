using System.Collections.Generic;
using JRogue.Item;
using JRogue.Manager.Inventory;
using JRogue.Stats;
using JRogue.Tests.UnitTests.Input;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.Inventory
{
    [TestFixture]
    public sealed class InventoryConsumeQuantityTests
    {
        readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _created)
            {
                if (go != null)
                    Object.DestroyImmediate(go);
            }

            _created.Clear();
        }

        [Test]
        public void TryConsumeCarriedQuantity_DecrementsStack()
        {
            GameObject owner = new GameObject("Owner");
            _created.Add(owner);
            owner.AddComponent<CharacterStats>();
            InventoryManager inv = owner.AddComponent<InventoryManager>();

            var def = ScriptableObject.CreateInstance<ItemData>();
            def.itemName = "Test Knife";
            def.weight = 0.1f;
            var stack = new ItemInstance(def, qty: 5);
            inv.AddItem(stack);

            Assert.IsTrue(inv.TryConsumeCarriedQuantity(stack, 1));
            Assert.AreEqual(4, stack.Quantity);
            Assert.AreEqual(1, inv.CarriedItems.Count);
        }

        [Test]
        public void TryConsumeCarriedQuantity_RemovesAtZero()
        {
            GameObject owner = new GameObject("Owner");
            _created.Add(owner);
            owner.AddComponent<CharacterStats>();
            InventoryManager inv = owner.AddComponent<InventoryManager>();

            var def = ScriptableObject.CreateInstance<ItemData>();
            def.weight = 0.1f;
            var stack = new ItemInstance(def, qty: 1);
            inv.AddItem(stack);

            Assert.IsTrue(inv.TryConsumeCarriedQuantity(stack, 1));
            Assert.AreEqual(0, inv.CarriedItems.Count);
        }

        [Test]
        public void TryConsumeCarriedQuantity_RejectsOverConsume()
        {
            GameObject owner = new GameObject("Owner");
            _created.Add(owner);
            owner.AddComponent<CharacterStats>();
            InventoryManager inv = owner.AddComponent<InventoryManager>();

            var def = ScriptableObject.CreateInstance<ItemData>();
            def.weight = 0.1f;
            var stack = new ItemInstance(def, qty: 2);
            inv.AddItem(stack);

            Assert.IsFalse(inv.TryConsumeCarriedQuantity(stack, 3));
            Assert.AreEqual(2, stack.Quantity);
        }
    }
}
