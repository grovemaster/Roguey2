using System.Collections.Generic;
using JRogue.Item;
using JRogue.Manager.Equipment;
using JRogue.Stats;
using JRogue.Stats.Racial;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.Equipment
{
    public class EquipmentLegalityEvaluatorTests
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
        public void SlotMismatch_Fails()
        {
            GameObject go = new GameObject("SlotMismatch");
            _destroy.Add(go);
            go.AddComponent<CharacterStats>();
            ItemData item = ScriptableObject.CreateInstance<ItemData>();
            _destroy.Add(item);
            item.slotType = EquipmentSlot.Head;

            Assert.IsFalse(
                EquipmentLegalityEvaluator.CanEquip(go, item, EquipmentSlot.Torso, out string reason));
            Assert.IsNotEmpty(reason);
        }

        [Test]
        public void Horns_WithHelmetExcludingHorns_Fails()
        {
            GameObject go = new GameObject("Horned");
            _destroy.Add(go);
            CharacterStats stats = go.AddComponent<CharacterStats>();
            stats.bodyCapabilities = BodyCapabilityFlags.Horns;

            ItemData helmet = ScriptableObject.CreateInstance<ItemData>();
            _destroy.Add(helmet);
            helmet.slotType = EquipmentSlot.Head;
            helmet.equipExcludesActorFlags = BodyCapabilityFlags.Horns;

            Assert.IsFalse(
                EquipmentLegalityEvaluator.CanEquip(go, helmet, EquipmentSlot.Head, out _));
        }

        [Test]
        public void Horns_WithBypassContribution_AllowsHelmet()
        {
            GameObject go = new GameObject("Bypass");
            _destroy.Add(go);
            CharacterStats stats = go.AddComponent<CharacterStats>();
            stats.bodyCapabilities = BodyCapabilityFlags.Horns;
            stats.RegisterBodyEquipmentContribution(
                "EssenceSlot:test",
                BodyCapabilityFlags.None,
                BodyCapabilityFlags.Horns);

            ItemData helmet = ScriptableObject.CreateInstance<ItemData>();
            _destroy.Add(helmet);
            helmet.slotType = EquipmentSlot.Head;
            helmet.equipExcludesActorFlags = BodyCapabilityFlags.Horns;

            Assert.IsTrue(
                EquipmentLegalityEvaluator.CanEquip(go, helmet, EquipmentSlot.Head, out string reason),
                reason);
        }

        [Test]
        public void MissingRequiredCapability_Fails()
        {
            GameObject go = new GameObject("NoReq");
            _destroy.Add(go);
            go.AddComponent<CharacterStats>();

            ItemData item = ScriptableObject.CreateInstance<ItemData>();
            _destroy.Add(item);
            item.slotType = EquipmentSlot.Feet;
            item.equipRequiresAllFlags = BodyCapabilityFlags.ReducedStature;

            Assert.IsFalse(
                EquipmentLegalityEvaluator.CanEquip(go, item, EquipmentSlot.Feet, out _));
        }

        [Test]
        public void RequiredIntrinsic_Passes()
        {
            GameObject go = new GameObject("ReqOk");
            _destroy.Add(go);
            CharacterStats stats = go.AddComponent<CharacterStats>();
            stats.bodyCapabilities = BodyCapabilityFlags.ReducedStature;

            ItemData item = ScriptableObject.CreateInstance<ItemData>();
            _destroy.Add(item);
            item.slotType = EquipmentSlot.Feet;
            item.equipRequiresAllFlags = BodyCapabilityFlags.ReducedStature;

            Assert.IsTrue(
                EquipmentLegalityEvaluator.CanEquip(go, item, EquipmentSlot.Feet, out string reason),
                reason);
        }

        [Test]
        public void RequiredFromEssenceOrMask_Passes()
        {
            GameObject go = new GameObject("ReqFromEssence");
            _destroy.Add(go);
            CharacterStats stats = go.AddComponent<CharacterStats>();
            stats.RegisterBodyEquipmentContribution(
                "EssenceSlot:0",
                BodyCapabilityFlags.ReducedStature,
                BodyCapabilityFlags.None);

            ItemData item = ScriptableObject.CreateInstance<ItemData>();
            _destroy.Add(item);
            item.slotType = EquipmentSlot.Feet;
            item.equipRequiresAllFlags = BodyCapabilityFlags.ReducedStature;

            Assert.IsTrue(
                EquipmentLegalityEvaluator.CanEquip(go, item, EquipmentSlot.Feet, out string reason),
                reason);
        }
    }
}
