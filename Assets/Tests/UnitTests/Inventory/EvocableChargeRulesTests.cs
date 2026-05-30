using JRogue.Item;
using JRogue.Manager.Inventory;
using JRogue.Stats;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.Inventory
{
    public class EvocableChargeRulesTests
    {
        [Test]
        public void InitializeCharges_ClampsStartingToMax()
        {
            var def = ScriptableObject.CreateInstance<EvocableItemData>();
            def.maxCharges = 2;
            def.startingCharges = 99;

            var inst = new ItemInstance(def, 1);
            Assert.AreEqual(2, inst.CurrentCharges);
            Assert.AreEqual(2, inst.MaxCharges);
            Assert.AreEqual(1, inst.Quantity);
            Object.DestroyImmediate(def);
        }

        [Test]
        public void SpendCharge_ConsumableAtZero_RemovesFromInventory()
        {
            var def = ScriptableObject.CreateInstance<EvocableItemData>();
            def.maxCharges = 2;
            def.startingCharges = 1;
            def.consumesWhenEmpty = true;
            def.invokeAbility = null;

            InventoryManager inv = CreateInventoryWithStats(out GameObject go);
            var inst = ItemInstance.CreateEvocable(def, 1);
            inv.AddItem(inst);

            EvocableChargeRules.SpendChargeAfterSuccessfulInvoke(inv, inst);

            Assert.AreEqual(0, inv.CarriedItems.Count);
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(def);
        }

        [Test]
        public void SpendCharge_RechargeableAtZero_KeepsInstance()
        {
            var def = ScriptableObject.CreateInstance<EvocableItemData>();
            def.maxCharges = 4;
            def.startingCharges = 1;
            def.consumesWhenEmpty = false;

            InventoryManager inv = CreateInventoryWithStats(out GameObject go);
            var inst = ItemInstance.CreateEvocable(def, 1);
            inv.AddItem(inst);

            EvocableChargeRules.SpendChargeAfterSuccessfulInvoke(inv, inst);

            Assert.AreEqual(1, inv.CarriedItems.Count);
            Assert.AreEqual(0, inst.CurrentCharges);
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(def);
        }

        [Test]
        public void RechargeService_AfterInterval_AddsOneCharge()
        {
            var def = ScriptableObject.CreateInstance<EvocableItemData>();
            def.maxCharges = 4;
            def.consumesWhenEmpty = false;
            def.rechargeIntervalPlayerPhases = 3;

            InventoryManager inv = CreateInventoryWithStats(out GameObject go);
            var inst = ItemInstance.CreateEvocable(def, 0);
            inv.AddItem(inst);

            EvocableRechargeService.TickInventoryForTests(inv);
            Assert.AreEqual(0, inst.CurrentCharges);
            Assert.AreEqual(1, inst.RechargePhasesAccumulated);

            EvocableRechargeService.TickInventoryForTests(inv);
            EvocableRechargeService.TickInventoryForTests(inv);
            Assert.AreEqual(1, inst.CurrentCharges);
            Assert.AreEqual(0, inst.RechargePhasesAccumulated);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(def);
        }

        [Test]
        public void CreateFromDefinition_TwoInstances_DoNotShareChargeState()
        {
            var def = ScriptableObject.CreateInstance<EvocableItemData>();
            def.maxCharges = 2;
            def.startingCharges = 2;

            var a = ItemInstance.CreateEvocable(def, 2);
            var b = ItemInstance.CreateEvocable(def, 1);
            Assert.AreNotEqual(a.Id, b.Id);
            Assert.AreEqual(2, a.CurrentCharges);
            Assert.AreEqual(1, b.CurrentCharges);
            Object.DestroyImmediate(def);
        }

        static InventoryManager CreateInventoryWithStats(out GameObject root)
        {
            root = new GameObject("inv");
            var stats = root.AddComponent<CharacterStats>();
            stats.Constitution = new Stat(200);
            return root.AddComponent<InventoryManager>();
        }
    }
}
