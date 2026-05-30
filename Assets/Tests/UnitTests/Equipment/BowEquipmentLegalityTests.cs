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
    public sealed class BowEquipmentLegalityTests
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
        public void CanEquip_ShieldBlockedWhileBowWielded()
        {
            GameObject actor = CreateActor();
            EquipmentManager equip = actor.GetComponent<EquipmentManager>();
            ItemData bow = CreateBow();
            ItemData arrow = CreateArrow();
            ItemData shield = CreateOffHandItem("Wooden Shield", requiresBow: false);

            var bowInst = new ItemInstance(bow, 1);
            var arrowInst = new ItemInstance(arrow, 10);
            actor.GetComponent<InventoryManager>().AddItem(bowInst);
            actor.GetComponent<InventoryManager>().AddItem(arrowInst);
            equip.EquipItem(EquipmentSlot.MainHand, bowInst);
            equip.EquipItem(EquipmentSlot.OffHand, arrowInst);

            Assert.IsFalse(
                EquipmentLegalityEvaluator.CanEquip(actor, shield, EquipmentSlot.OffHand, out string reason),
                reason);
            StringAssert.Contains("arrow ammo", reason);
        }

        [Test]
        public void CanEquip_ArrowRequiresBowInMainHand()
        {
            GameObject actor = CreateActor();
            ItemData arrow = CreateArrow();

            Assert.IsFalse(
                EquipmentLegalityEvaluator.CanEquip(actor, arrow, EquipmentSlot.OffHand, out string reason),
                reason);
            StringAssert.Contains("bow", reason);
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

        ItemData CreateOffHandItem(string name, bool requiresBow)
        {
            var item = ScriptableObject.CreateInstance<ItemData>();
            _assets.Add(item);
            item.itemName = name;
            item.category = ItemCategory.Armor;
            item.slotType = EquipmentSlot.OffHand;
            item.requiresBow = requiresBow;
            return item;
        }
    }
}
