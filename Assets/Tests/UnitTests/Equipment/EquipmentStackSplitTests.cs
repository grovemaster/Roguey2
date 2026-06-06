using System.Collections.Generic;
using JRogue.Item;
using JRogue.Manager.Equipment;
using JRogue.Manager.Inventory;
using JRogue.Stats;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.Equipment
{
    [TestFixture]
    public sealed class EquipmentStackSplitTests
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
        public void EquipFromStack_SplitsOneUnitIntoEquippedRow()
        {
            GameObject actor = CreateActor();
            InventoryManager inv = actor.GetComponent<InventoryManager>();
            EquipmentManager equip = actor.GetComponent<EquipmentManager>();
            ItemData blade = CreateWeapon("Giant's Blade");

            var stack = new ItemInstance(blade, 2);
            inv.AddItem(stack);

            equip.EquipItem(EquipmentSlot.MainHand, stack);

            ItemInstance equipped = equip.GetEquippedInstance(EquipmentSlot.MainHand);
            Assert.NotNull(equipped);
            Assert.AreEqual(1, equipped.Quantity);
            Assert.AreNotEqual(stack.Id, equipped.Id);
            Assert.AreEqual(1, inv.CarriedItems.Count);
            Assert.AreEqual(1, inv.CarriedItems[0].Quantity);
            Assert.AreEqual(stack.Id, inv.CarriedItems[0].Id);
        }

        [Test]
        public void Unequip_MergesIntoExistingCarriedStack()
        {
            GameObject actor = CreateActor();
            InventoryManager inv = actor.GetComponent<InventoryManager>();
            EquipmentManager equip = actor.GetComponent<EquipmentManager>();
            ItemData blade = CreateWeapon("Giant's Blade");

            var stack = new ItemInstance(blade, 2);
            inv.AddItem(stack);
            equip.EquipItem(EquipmentSlot.MainHand, stack);

            Assert.IsTrue(equip.TryUnequipToBag(EquipmentSlot.MainHand));

            Assert.AreEqual(1, inv.CarriedItems.Count);
            Assert.AreEqual(2, inv.CarriedItems[0].Quantity);
            Assert.AreEqual(stack.Id, inv.CarriedItems[0].Id);
            Assert.IsNull(equip.GetEquippedInstance(EquipmentSlot.MainHand));
        }

        [Test]
        public void EquipBowAmmo_MovesWholeStack()
        {
            GameObject actor = CreateActor();
            InventoryManager inv = actor.GetComponent<InventoryManager>();
            EquipmentManager equip = actor.GetComponent<EquipmentManager>();
            ItemData bow = CreateBow();
            ItemData arrow = CreateArrow();

            var bowInst = new ItemInstance(bow, 1);
            var arrowStack = new ItemInstance(arrow, 5);
            inv.AddItem(bowInst);
            inv.AddItem(arrowStack);
            equip.EquipItem(EquipmentSlot.MainHand, bowInst);
            equip.EquipItem(EquipmentSlot.OffHand, arrowStack);

            Assert.AreEqual(0, inv.CarriedItems.Count);
            Assert.AreEqual(5, equip.GetEquippedInstance(EquipmentSlot.OffHand).Quantity);
            Assert.AreEqual(arrowStack.Id, equip.GetEquippedInstance(EquipmentSlot.OffHand).Id);
        }

        GameObject CreateActor()
        {
            GameObject go = new GameObject("Actor");
            _created.Add(go);
            go.AddComponent<CharacterStats>();
            go.AddComponent<InventoryManager>();
            go.AddComponent<EquipmentManager>();
            return go;
        }

        ItemData CreateWeapon(string name)
        {
            var weapon = ScriptableObject.CreateInstance<ItemData>();
            _assets.Add(weapon);
            weapon.itemName = name;
            weapon.category = ItemCategory.Weapon;
            weapon.slotType = EquipmentSlot.MainHand;
            weapon.weight = 1f;
            return weapon;
        }

        ItemData CreateBow()
        {
            var bow = ScriptableObject.CreateInstance<ItemData>();
            _assets.Add(bow);
            bow.itemName = "Short Bow";
            bow.category = ItemCategory.Weapon;
            bow.slotType = EquipmentSlot.MainHand;
            bow.weaponType = WeaponType.Bow;
            return bow;
        }

        ItemData CreateArrow()
        {
            var arrow = ScriptableObject.CreateInstance<ItemData>();
            _assets.Add(arrow);
            arrow.itemName = "Stone Arrow";
            arrow.category = ItemCategory.Missile;
            arrow.slotType = EquipmentSlot.OffHand;
            arrow.requiresBow = true;
            return arrow;
        }
    }
}
