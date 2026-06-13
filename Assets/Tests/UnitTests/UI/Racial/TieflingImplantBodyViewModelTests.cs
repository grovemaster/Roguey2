using System.Collections.Generic;
using JRogue.Ability;
using JRogue.Actors;
using JRogue.Item.Essence;
using JRogue.Racial;
using JRogue.Stats;
using JRogue.Stats.Racial;
using JRogue.Tests.Mocks;
using JRogue.UI.Racial;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UI.Racial
{
    public class TieflingImplantBodyViewModelTests
    {
        sealed class TestPartyActor : BaseActor
        {
            protected override void Die() { }
        }

        sealed class TestPassiveEffect : PassiveEffect
        {
            public override void OnApply(GameObject user) { }
            public override void OnRemove(GameObject user) { }
        }

        readonly List<Object> _toDestroy = new();

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _toDestroy)
            {
                if (o != null)
                    Object.DestroyImmediate(o);
            }

            _toDestroy.Clear();
        }

        [Test]
        public void DefaultSelection_PrefersLeftArm_WhenOccupied()
        {
            GameObject go = CreateTiefling(
                preset(ImplantSlot.LeftArm, CreateImplant("iron_sleeve", ImplantSlot.LeftArm)),
                preset(ImplantSlot.Torso, CreateImplant("thoracic_plate", ImplantSlot.Torso)));
            go.GetComponent<TieflingImplantsRuntime>().TryApplyPresetFromSerialized();

            Assert.AreEqual(
                ImplantSlot.LeftArm,
                TieflingImplantBodyViewModel.ResolveDefaultSelection(go.GetComponent<TieflingImplantsRuntime>()));
        }

        [Test]
        public void DefaultSelection_FallsBackToFirstOccupied_WhenLeftArmEmpty()
        {
            GameObject go = CreateTiefling(
                preset(ImplantSlot.Torso, CreateImplant("thoracic_plate", ImplantSlot.Torso)));
            go.GetComponent<TieflingImplantsRuntime>().TryApplyPresetFromSerialized();

            Assert.AreEqual(
                ImplantSlot.Torso,
                TieflingImplantBodyViewModel.ResolveDefaultSelection(go.GetComponent<TieflingImplantsRuntime>()));
        }

        [Test]
        public void DefaultSelection_UsesLeftArm_WhenAllSlotsEmpty()
        {
            GameObject go = CreateTiefling();

            Assert.AreEqual(
                ImplantSlot.LeftArm,
                TieflingImplantBodyViewModel.ResolveDefaultSelection(go.GetComponent<TieflingImplantsRuntime>()));
        }

        [Test]
        public void Build_OccupiedLeftArm_ShowsStrengthAndActiveInDetail()
        {
            CyborgImplantDefinition arm = CreateImplant("iron_sleeve", ImplantSlot.LeftArm, StatType.Strength, 10);
            arm.displayName = "Iron Sleeve";
            arm.description = "Reinforced left-arm cybernetic sleeve.";
            arm.passiveEffects = new List<PassiveEffect>();
            var passive = ScriptableObject.CreateInstance<TestPassiveEffect>();
            _toDestroy.Add(passive);
            passive.name = "Enhanced Limb";
            passive.effectDescription = "Reinforced arm.";
            arm.passiveEffects.Add(passive);
            var active = ScriptableObject.CreateInstance<DummyTargetAbility>();
            _toDestroy.Add(active);
            active.abilityName = "Sudden Strength";
            active.description = "Burst of power.";
            active.soulPowerCost = 1;
            arm.activeAbilities = new List<AbilityAction> { active };

            GameObject go = CreateTiefling(preset(ImplantSlot.LeftArm, arm));
            go.GetComponent<TieflingImplantsRuntime>().TryApplyPresetFromSerialized();
            var actor = go.AddComponent<TestPartyActor>();
            actor.stats = go.GetComponent<CharacterStats>();

            TieflingImplantBodyViewModel vm = TieflingImplantBodyViewModel.Build(actor, ImplantSlot.LeftArm);

            Assert.AreEqual(7, vm.Cells.Count);
            Assert.IsTrue(FindCell(vm, ImplantSlot.LeftArm).Occupied);
            Assert.IsFalse(FindCell(vm, ImplantSlot.Heart).Occupied);
            StringAssert.Contains("Iron Sleeve", vm.Detail.HeroTitle);
            StringAssert.Contains("+10 Strength", vm.Detail.LeftColumnText);
            StringAssert.Contains("Sudden Strength", vm.Detail.RightColumnText);
            StringAssert.Contains("Enhanced Limb", vm.Detail.RightColumnText);
            StringAssert.Contains("ability hotbar", vm.Detail.RightColumnText);
        }

        [Test]
        public void Build_EmptyHeartSlot_ShowsEmptyCopy()
        {
            GameObject go = CreateTiefling();
            var actor = go.AddComponent<TestPartyActor>();
            actor.stats = go.GetComponent<CharacterStats>();

            TieflingImplantBodyViewModel vm = TieflingImplantBodyViewModel.Build(actor, ImplantSlot.Heart);

            Assert.IsFalse(vm.Detail.Occupied);
            StringAssert.Contains("Empty", vm.Detail.HeroSubtitle);
            StringAssert.Contains("Fleshmetal Forgemaster", vm.Detail.LeftColumnText);
            Assert.IsTrue(string.IsNullOrEmpty(vm.Detail.RightColumnText));
            Assert.IsFalse(vm.Detail.LeftColumnText.Contains("STATS"));
        }

        [Test]
        public void FolkBaseline_IncludesFireResistAndHorns()
        {
            var loadout = ScriptableObject.CreateInstance<RacialLoadoutDefinition>();
            _toDestroy.Add(loadout);
            loadout.resistanceModifiers = new List<DamageResistanceModifier>
            {
                new DamageResistanceModifier { type = DamageType.Fire, value = 10 }
            };

            var statsGo = new GameObject("Stats");
            _toDestroy.Add(statsGo);
            var stats = statsGo.AddComponent<CharacterStats>();
            stats.bodyCapabilities = BodyCapabilityFlags.Horns;

            string summary = TieflingImplantBodyViewModel.BuildFolkBaselineSummary(loadout, stats);

            StringAssert.Contains("FOLK BASELINE", summary);
            StringAssert.Contains("Fire resist +10", summary);
            StringAssert.Contains("Horns", summary);
        }

        static TieflingImplantSlotCellModel FindCell(TieflingImplantBodyViewModel vm, ImplantSlot slot)
        {
            foreach (TieflingImplantSlotCellModel cell in vm.Cells)
            {
                if (cell.Slot == slot)
                    return cell;
            }

            Assert.Fail($"Missing cell for {slot}");
            return null;
        }

        static ImplantSlotPreset preset(ImplantSlot slot, CyborgImplantDefinition implant) =>
            new ImplantSlotPreset { slot = slot, implant = implant };

        GameObject CreateTiefling(params ImplantSlotPreset[] presets)
        {
            var go = new GameObject("TieflingTest");
            _toDestroy.Add(go);
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

        static CyborgImplantDefinition CreateImplant(
            string id,
            ImplantSlot slot,
            StatType stat = StatType.Strength,
            int value = 1)
        {
            var def = ScriptableObject.CreateInstance<CyborgImplantDefinition>();
            def.implantId = id;
            def.displayName = id;
            def.allowedSlots = new List<ImplantSlot> { slot };
            def.statModifiers = new List<AttributeModifier>
            {
                new AttributeModifier { attribute = stat, value = value }
            };
            def.passiveEffects = new List<PassiveEffect>();
            def.activeAbilities = new List<AbilityAction>();
            return def;
        }
    }
}
