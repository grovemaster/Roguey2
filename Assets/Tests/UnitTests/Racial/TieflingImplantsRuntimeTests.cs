using System.Collections.Generic;
using JRogue.Ability.Passive;
using JRogue.Item;
using JRogue.Item.Essence;
using JRogue.Manager.Equipment;
using JRogue.Racial;
using JRogue.Stats;
using JRogue.Stats.Racial;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.Racial
{
    public class TieflingImplantsRuntimeTests
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
        public void Preset_AppliesImplantStats()
        {
            CyborgImplantDefinition arm = CreateArmImplant();
            CyborgImplantDefinition torso = CreateTorsoImplant();
            GameObject go = CreateTieflingWithPresets(
                new ImplantSlotPreset { slot = ImplantSlot.LeftArm, implant = arm },
                new ImplantSlotPreset { slot = ImplantSlot.Torso, implant = torso });
            var runtime = go.GetComponent<TieflingImplantsRuntime>();
            runtime.TryApplyPresetFromSerialized();

            CharacterStats stats = go.GetComponent<CharacterStats>();
            Assert.AreEqual(11, stats.Strength.GetValue());
            Assert.AreEqual(11, stats.Constitution.GetValue());
            Assert.IsTrue(runtime.TryGetInstalled(ImplantSlot.LeftArm, out _));
            Assert.IsTrue(runtime.TryGetInstalled(ImplantSlot.Torso, out _));
        }

        [Test]
        public void Replace_LeftArm_SwapsModifiers_OtherSlotUnchanged()
        {
            CyborgImplantDefinition armA = CreateArmImplant();
            CyborgImplantDefinition armB = CreateArmImplant("arm_b", StatType.Dexterity, 2);
            CyborgImplantDefinition torso = CreateTorsoImplant();
            GameObject go = CreateTieflingWithPresets(
                new ImplantSlotPreset { slot = ImplantSlot.LeftArm, implant = armA },
                new ImplantSlotPreset { slot = ImplantSlot.Torso, implant = torso });
            var runtime = go.GetComponent<TieflingImplantsRuntime>();
            runtime.TryApplyPresetFromSerialized();
            CharacterStats stats = go.GetComponent<CharacterStats>();

            Assert.IsTrue(runtime.TryReplaceImplant(ImplantSlot.LeftArm, armB, out _));
            Assert.AreEqual(10, stats.Strength.GetValue());
            Assert.AreEqual(12, stats.Dexterity.GetValue());
            Assert.AreEqual(11, stats.Constitution.GetValue());
        }

        [Test]
        public void Install_LeftArm_AppliesTenStrength()
        {
            CyborgImplantDefinition arm = CreateArmImplant(value: 10);
            GameObject go = CreateTieflingWithPresets(
                new ImplantSlotPreset { slot = ImplantSlot.LeftArm, implant = arm });
            go.GetComponent<TieflingImplantsRuntime>().TryApplyPresetFromSerialized();

            Assert.AreEqual(20, go.GetComponent<CharacterStats>().Strength.GetValue());
        }

        [Test]
        public void Install_InvalidSlot_Fails()
        {
            CyborgImplantDefinition heartOnly = CreateArmImplant();
            heartOnly.allowedSlots = new List<ImplantSlot> { ImplantSlot.Heart };
            GameObject go = CreateTieflingWithPresets();
            var runtime = go.GetComponent<TieflingImplantsRuntime>();

            Assert.IsFalse(runtime.TryInstallImplant(ImplantSlot.Torso, heartOnly, out string reason));
            Assert.IsNotEmpty(reason);
        }

        [Test]
        public void Baseline_FireResistance_WithoutImplants()
        {
            GameObject go = CreateTieflingWithPresets();
            var applier = go.GetComponent<RacialLoadoutApplier>();
            var loadout = ScriptableObject.CreateInstance<RacialLoadoutDefinition>();
            _destroy.Add(loadout);
            loadout.requiredRace = Race.Tiefling;
            loadout.resistanceModifiers = new List<DamageResistanceModifier>
            {
                new DamageResistanceModifier { type = DamageType.Fire, value = 10 }
            };
            applier.SetLoadout(loadout);

            Assert.AreEqual(10, go.GetComponent<CharacterStats>().GetResistance(DamageType.Fire));
        }

        [Test]
        public void Horns_CannotEquipHornExcludingHelmet()
        {
            GameObject go = CreateTieflingWithPresets();
            go.GetComponent<CharacterStats>().bodyCapabilities = BodyCapabilityFlags.Horns;

            ItemData helmet = ScriptableObject.CreateInstance<ItemData>();
            _destroy.Add(helmet);
            helmet.slotType = EquipmentSlot.Head;
            helmet.equipExcludesActorFlags = BodyCapabilityFlags.Horns;

            Assert.IsFalse(EquipmentLegalityEvaluator.CanEquip(go, helmet, EquipmentSlot.Head, out _));
        }

        GameObject CreateTieflingWithPresets(params ImplantSlotPreset[] presets)
        {
            var go = new GameObject("TieflingTest");
            _destroy.Add(go);
            var stats = go.AddComponent<CharacterStats>();
            stats.race = Race.Tiefling;
            stats.racialSubsystem = RacialSubsystemKind.TieflingImplants;
            stats.bodyCapabilities = BodyCapabilityFlags.Horns;
            go.AddComponent<RacialLoadoutApplier>();
            var runtime = go.AddComponent<TieflingImplantsRuntime>();
            var field = typeof(TieflingImplantsRuntime).GetField(
                "presetImplants",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(runtime, new List<ImplantSlotPreset>(presets));
            return go;
        }

        static CyborgImplantDefinition CreateArmImplant(
            string id = "iron_sleeve",
            StatType stat = StatType.Strength,
            int value = 1)
        {
            var def = ScriptableObject.CreateInstance<CyborgImplantDefinition>();
            def.implantId = id;
            def.allowedSlots = new List<ImplantSlot> { ImplantSlot.LeftArm };
            def.statModifiers = new List<AttributeModifier>
            {
                new AttributeModifier { attribute = stat, value = value }
            };
            def.passiveEffects = new List<PassiveEffect>();
            return def;
        }

        static CyborgImplantDefinition CreateTorsoImplant()
        {
            var def = ScriptableObject.CreateInstance<CyborgImplantDefinition>();
            def.implantId = "thoracic_plate";
            def.allowedSlots = new List<ImplantSlot> { ImplantSlot.Torso };
            def.statModifiers = new List<AttributeModifier>
            {
                new AttributeModifier { attribute = StatType.Constitution, value = 1 }
            };
            def.passiveEffects = new List<PassiveEffect>();
            return def;
        }
    }
}
