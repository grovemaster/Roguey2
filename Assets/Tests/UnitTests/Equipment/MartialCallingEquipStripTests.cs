using System.Collections.Generic;
using JRogue.Item;
using JRogue.Manager.Equipment;
using JRogue.Manager.Inventory;
using JRogue.Racial;
using JRogue.Stats;
using JRogue.Stats.Racial;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.Equipment
{
    public class MartialCallingEquipStripTests
    {
        readonly List<Object> _destroy = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _destroy)
            {
                if (o != null)
                    Object.DestroyImmediate(o);
            }

            _destroy.Clear();
        }

        [Test]
        public void ClassCommitToMage_StripsMartialCallingGearToBag()
        {
            GameObject go = CreateHumanActor();
            EquipmentManager equip = go.GetComponent<EquipmentManager>();
            InventoryManager inv = go.GetComponent<InventoryManager>();

            ItemData sword = CreateMartialWeapon();
            ItemInstance inst = new ItemInstance(sword);
            inv.TryStowCarriedItem(inst);
            equip.EquipItem(EquipmentSlot.MainHand, inst);

            Assert.IsTrue(equip.GetEquippedInstance(EquipmentSlot.MainHand) != null);

            Assert.IsTrue(HumanClassCommitment.TryCommit(go, HumanClass.Mage, out string error), error);

            Assert.IsNull(equip.GetEquippedInstance(EquipmentSlot.MainHand));
            Assert.AreEqual(1, inv.CarriedItems.Count);
            Assert.AreEqual(ItemStorageLocation.Carried, inv.CarriedItems[0].StorageLocation);
        }

        GameObject CreateHumanActor()
        {
            GameObject go = new GameObject("HumanStrip");
            _destroy.Add(go);
            CharacterStats stats = go.AddComponent<CharacterStats>();
            stats.race = Race.Human;
            stats.humanClass = HumanClass.None;
            go.AddComponent<InventoryManager>();
            go.AddComponent<EquipmentManager>();
            return go;
        }

        ItemData CreateMartialWeapon()
        {
            ItemData item = ScriptableObject.CreateInstance<ItemData>();
            _destroy.Add(item);
            item.itemName = "Giant's Blade";
            item.category = ItemCategory.Weapon;
            item.slotType = EquipmentSlot.MainHand;
            item.requiresMartialCalling = true;
            return item;
        }
    }
}
