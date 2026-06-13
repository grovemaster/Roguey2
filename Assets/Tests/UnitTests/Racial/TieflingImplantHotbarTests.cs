using System.Collections.Generic;
using JRogue.Ability;
using JRogue.Ability.SuddenStrength;
using JRogue.Actors;
using JRogue.Item.Essence;
using JRogue.Manager.Essence;
using JRogue.Racial;
using JRogue.Stats;
using JRogue.Stats.Racial;
using JRogue.UI.Hotbar;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.Racial
{
    [TestFixture]
    public sealed class TieflingImplantHotbarTests
    {
        readonly List<Object> _cleanup = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object asset in _cleanup)
            {
                if (asset != null)
                    Object.DestroyImmediate(asset);
            }

            _cleanup.Clear();
        }

        [Test]
        public void BuildPool_IncludesSuddenStrengthFromInstalledImplant()
        {
            BaseActor tiefling = CreateTieflingWithImplant(out SuddenStrengthAbility ability);
            List<(HotbarEntry entry, string displayName, string group)> pool =
                HotbarAssignabilityService.BuildPool(tiefling);

            Assert.IsTrue(pool.Exists(entry =>
                entry.entry.Kind == HotbarEntryKind.RacialActive
                && entry.entry.racialBindingKey
                    == HotbarResolver.BuildTieflingImplantActiveBindingKey(ImplantSlot.LeftArm, 0)));
        }

        [Test]
        public void BuildPool_SkipsImplantSuddenStrengthWhenEssenceAlreadyGrantsIt()
        {
            BaseActor tiefling = CreateTieflingWithImplant(out SuddenStrengthAbility ability);
            EssenceSlotManager essence = tiefling.GetComponent<EssenceSlotManager>();
            var essenceData = ScriptableObject.CreateInstance<EssenceData>();
            _cleanup.Add(essenceData);
            essenceData.essenceName = "Sudden Strength";
            essenceData.activeAbilities = new List<AbilityAction> { ability };
            essence.EquipEssence(essenceData, 0);

            List<(HotbarEntry entry, string displayName, string group)> pool =
                HotbarAssignabilityService.BuildPool(tiefling);

            int suddenStrengthEntries = 0;
            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i].entry.abilityAssetName == ability.name)
                    suddenStrengthEntries++;
            }

            Assert.AreEqual(1, suddenStrengthEntries);
            Assert.IsTrue(pool.Exists(entry => entry.entry.Kind == HotbarEntryKind.EssenceActive));
            Assert.IsFalse(pool.Exists(entry =>
                entry.entry.racialBindingKey
                    == HotbarResolver.BuildTieflingImplantActiveBindingKey(ImplantSlot.LeftArm, 0)));
        }

        [Test]
        public void Resolve_TieflingImplantActive_ReturnsInstalledAbility()
        {
            BaseActor tiefling = CreateTieflingWithImplant(out SuddenStrengthAbility ability);

            HotbarResolvedAction resolved = HotbarResolver.Resolve(
                tiefling,
                new HotbarEntry
                {
                    Kind = HotbarEntryKind.RacialActive,
                    racialBindingKey = HotbarResolver.BuildTieflingImplantActiveBindingKey(
                        ImplantSlot.LeftArm,
                        0),
                });

            Assert.IsTrue(resolved.IsValid);
            Assert.IsFalse(resolved.IsStale);
            Assert.AreSame(ability, resolved.Ability);
        }

        BaseActor CreateTieflingWithImplant(out SuddenStrengthAbility ability)
        {
            var go = new GameObject("TieflingHotbarTest");
            _cleanup.Add(go);
            go.AddComponent<BaseActor>();
            var stats = go.AddComponent<CharacterStats>();
            stats.race = Race.Tiefling;
            stats.racialSubsystem = RacialSubsystemKind.TieflingImplants;

            ability = ScriptableObject.CreateInstance<SuddenStrengthAbility>();
            ability.name = "SuddenStrength_Standard";
            _cleanup.Add(ability);

            CyborgImplantDefinition implant = CreateArmImplant(ability);
            var runtime = go.AddComponent<TieflingImplantsRuntime>();
            runtime.TryInstallImplant(ImplantSlot.LeftArm, implant, out _);
            go.AddComponent<EssenceSlotManager>();

            return go.GetComponent<BaseActor>();
        }

        static CyborgImplantDefinition CreateArmImplant(SuddenStrengthAbility ability)
        {
            var def = ScriptableObject.CreateInstance<CyborgImplantDefinition>();
            def.implantId = "iron_sleeve";
            def.displayName = "Iron Sleeve";
            def.allowedSlots = new List<ImplantSlot> { ImplantSlot.LeftArm };
            def.statModifiers = new List<AttributeModifier>
            {
                new AttributeModifier { attribute = StatType.Strength, value = 10 }
            };
            def.activeAbilities = new List<AbilityAction> { ability };
            return def;
        }
    }
}
