using System.Collections.Generic;
using JRogue.Combat;
using JRogue.Actors;
using JRogue.Item;
using JRogue.Manager.Equipment;
using JRogue.Manager.Inventory;
using JRogue.Stats;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.Combat
{
    [TestFixture]
    public sealed class BowRangedCombatTests
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
        public void ComputeBowShotDamage_StoneArrow_MatchesDcssBaseline()
        {
            GameObject actor = CreateActorWithStats(bowProf: 0, athletics: 0);
            ItemData bow = CreateBow(damage: 8);
            ItemData stone = CreateArrow(2, "Stone");

            int damage = BowRangedCombatService.ComputeBowShotDamage(
                actor.GetComponent<TestBowActor>(),
                bow,
                stone);

            Assert.AreEqual(10, damage);
        }

        [Test]
        public void ComputeBowShotDamage_AppliesSkillModifiers()
        {
            GameObject actor = CreateActorWithStats(bowProf: 25, athletics: 0);
            ItemData bow = CreateBow(damage: 8);
            ItemData steel = CreateArrow(4, "Steel");

            int damage = BowRangedCombatService.ComputeBowShotDamage(
                actor.GetComponent<TestBowActor>(),
                bow,
                steel);

            Assert.AreEqual(24, damage);
        }

        [Test]
        public void TryConsumeEquippedAmmo_PromotesNextStack()
        {
            GameObject owner = CreateActorWithStats(0, 0);
            InventoryManager inv = owner.GetComponent<InventoryManager>();
            EquipmentManager equip = owner.GetComponent<EquipmentManager>();

            ItemData bowDef = CreateBow(8);
            ItemData stoneDef = CreateArrow(2, "Stone");
            ItemData steelDef = CreateArrow(4, "Steel");
            var bow = new ItemInstance(bowDef, 1);
            var stone = new ItemInstance(stoneDef, 1);
            var steel = new ItemInstance(steelDef, 5);

            inv.AddItem(bow);
            inv.AddItem(stone);
            inv.AddItem(steel);
            equip.EquipItem(EquipmentSlot.MainHand, bow);
            equip.EquipItem(EquipmentSlot.OffHand, stone);

            Assert.IsTrue(equip.TryConsumeEquippedAmmo(1, out ItemData consumed));
            Assert.AreEqual(stoneDef, consumed);
            Assert.AreEqual(steelDef, equip.GetItemFromEquipmentSlot(EquipmentSlot.OffHand));
            Assert.AreEqual(5, equip.GetEquippedInstance(EquipmentSlot.OffHand).Quantity);
        }

        GameObject CreateActorWithStats(int bowProf, int athletics)
        {
            GameObject go = new GameObject("BowActor");
            _created.Add(go);
            var stats = go.AddComponent<CharacterStats>();
            foreach (WeaponType type in System.Enum.GetValues(typeof(WeaponType)))
                stats.WeaponProficiencies[type] = new Stat(0);
            foreach (SkillType type in System.Enum.GetValues(typeof(SkillType)))
                stats.Skills[type] = new Stat(0);
            stats.WeaponProficiencies[WeaponType.Bow] = new Stat(bowProf);
            stats.Skills[SkillType.Athletics] = new Stat(athletics);
            go.AddComponent<InventoryManager>();
            go.AddComponent<EquipmentManager>();
            go.AddComponent<TestBowActor>();
            return go;
        }

        sealed class TestBowActor : BaseActor
        {
            protected override void Die() { }
        }

        ItemData CreateBow(int damage)
        {
            var bow = ScriptableObject.CreateInstance<ItemData>();
            _assets.Add(bow);
            bow.itemName = "Test Bow";
            bow.category = ItemCategory.Weapon;
            bow.slotType = EquipmentSlot.MainHand;
            bow.weaponType = WeaponType.Bow;
            bow.damageModules = new List<DamageEntry> { new DamageEntry { type = DamageType.Pierce, value = damage } };
            return bow;
        }

        ItemData CreateArrow(int damage, string name)
        {
            var arrow = ScriptableObject.CreateInstance<ItemData>();
            _assets.Add(arrow);
            arrow.itemName = name;
            arrow.category = ItemCategory.Missile;
            arrow.slotType = EquipmentSlot.OffHand;
            arrow.requiresBow = true;
            arrow.isThrowable = false;
            arrow.damageModules = new List<DamageEntry> { new DamageEntry { type = DamageType.Pierce, value = damage } };
            return arrow;
        }
    }
}
