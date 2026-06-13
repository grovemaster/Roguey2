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
    public class BeastmanSoulBeastBodyViewModelTests
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

        readonly List<Object> _toDestroy = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _toDestroy)
            {
                if (o != null)
                    Object.DestroyImmediate(o);
            }

            _toDestroy.Clear();
            SoulBeastRegistryService.ResetRegistryForTests();
        }

        [Test]
        public void Build_Unbonded_ReturnsBlankState()
        {
            GameObject go = CreateUnbondedBeastman();
            var actor = go.AddComponent<TestPartyActor>();
            actor.stats = go.GetComponent<CharacterStats>();

            BeastmanSoulBeastBodyViewModel vm = BeastmanSoulBeastBodyViewModel.Build(actor);

            Assert.IsFalse(vm.IsBonded);
            Assert.AreEqual(BeastmanSoulBeastBodyViewModel.UnbondedTitle, vm.EmptyStateTitle);
            StringAssert.Contains("Soul Beast Ritual Circle", vm.EmptyStateBody);
            Assert.AreEqual(0, vm.AbilityRows.Count);
            Assert.IsFalse(vm.ShowEmptyAbilitiesHint);
        }

        [Test]
        public void Build_BondedStatsOnly_ShowsCumulativeStatsAndEmptyAbilitiesHint()
        {
            SoulBeastDefinition beast = CreateBeastWithStatsOnly();
            CreateRegistry(beast);
            GameObject go = CreateBondedBeastman(beast, startingLevel: 2);
            var actor = go.AddComponent<TestPartyActor>();
            actor.stats = go.GetComponent<CharacterStats>();
            actor.stats.level = 5;

            BeastmanSoulBeastBodyViewModel vm = BeastmanSoulBeastBodyViewModel.Build(actor);

            Assert.IsTrue(vm.IsBonded);
            Assert.AreEqual("Ember Wolf", vm.Summary.Title);
            StringAssert.Contains("Level 2 / Cap 5", vm.Summary.Subtitle);
            StringAssert.Contains("+2 Strength", vm.Summary.StatsLine);
            Assert.IsTrue(vm.ShowEmptyAbilitiesHint);
            Assert.AreEqual(0, vm.AbilityRows.Count);
            StringAssert.Contains("Beast Blood", vm.Summary.ProgressHint);
        }

        [Test]
        public void Build_BondedWithAbilities_FlattensPassivesBeforeActivesByLevel()
        {
            SoulBeastDefinition beast = CreateBeastWithAbilities();
            CreateRegistry(beast);
            GameObject go = CreateBondedBeastman(beast, startingLevel: 3);
            var actor = go.AddComponent<TestPartyActor>();
            actor.stats = go.GetComponent<CharacterStats>();
            actor.stats.level = 5;

            BeastmanSoulBeastBodyViewModel vm = BeastmanSoulBeastBodyViewModel.Build(actor);

            Assert.AreEqual(3, vm.AbilityRows.Count);
            Assert.AreEqual(BeastmanSoulBeastAbilityKind.Passive, vm.AbilityRows[0].Kind);
            Assert.AreEqual("Wolf's Endurance", vm.AbilityRows[0].Title);
            Assert.AreEqual("Level 2 · Passive", vm.AbilityRows[0].LevelTag);

            Assert.AreEqual(BeastmanSoulBeastAbilityKind.Passive, vm.AbilityRows[1].Kind);
            Assert.AreEqual("Level 3 · Passive", vm.AbilityRows[1].LevelTag);

            Assert.AreEqual(BeastmanSoulBeastAbilityKind.Active, vm.AbilityRows[2].Kind);
            Assert.AreEqual("Ember Rush", vm.AbilityRows[2].Title);
            Assert.AreEqual("Level 3 · Active", vm.AbilityRows[2].LevelTag);
            StringAssert.Contains("Soul 2", vm.AbilityRows[2].Meta);
            Assert.IsTrue(vm.AbilityRows[2].ShowHotbarFootnote);
            Assert.IsFalse(vm.ShowEmptyAbilitiesHint);
        }

        [Test]
        public void Build_AtCap_ShowsCappedProgressHint()
        {
            SoulBeastDefinition beast = CreateBeastWithStatsOnly();
            beast.maxLevel = 2;
            CreateRegistry(beast);
            GameObject go = CreateBondedBeastman(beast, startingLevel: 2);
            var actor = go.AddComponent<TestPartyActor>();
            actor.stats = go.GetComponent<CharacterStats>();
            actor.stats.level = 2;

            BeastmanSoulBeastBodyViewModel vm = BeastmanSoulBeastBodyViewModel.Build(actor);

            StringAssert.Contains("maximum", vm.Summary.ProgressHint);
            StringAssert.Contains("Level 2 / Cap 2", vm.Summary.Subtitle);
        }

        SoulBeastDefinition CreateBeastWithStatsOnly()
        {
            var beast = ScriptableObject.CreateInstance<SoulBeastDefinition>();
            _toDestroy.Add(beast);
            beast.soulBeastId = "ember_wolf";
            beast.displayName = "Ember Wolf";
            beast.soulBeastType = SoulBeastType.Enhancement;
            beast.maxLevel = 5;
            beast.levels = new List<SoulBeastLevelData>
            {
                new SoulBeastLevelData
                {
                    statModifiers = new List<AttributeModifier>
                    {
                        new AttributeModifier { attribute = StatType.Strength, value = 1 },
                    },
                },
                new SoulBeastLevelData
                {
                    statModifiers = new List<AttributeModifier>
                    {
                        new AttributeModifier { attribute = StatType.Strength, value = 1 },
                    },
                },
            };
            return beast;
        }

        SoulBeastDefinition CreateBeastWithAbilities()
        {
            var beast = CreateBeastWithStatsOnly();
            var passiveLv2 = ScriptableObject.CreateInstance<TestPassiveEffect>();
            _toDestroy.Add(passiveLv2);
            passiveLv2.name = "Wolf's Endurance";
            passiveLv2.effectDescription = "Steadies your frame against blows.";

            var passiveLv3 = ScriptableObject.CreateInstance<TestPassiveEffect>();
            _toDestroy.Add(passiveLv3);
            passiveLv3.name = "Ember Hide";
            passiveLv3.effectDescription = "Heat wards your skin.";

            var active = ScriptableObject.CreateInstance<DummyTargetAbility>();
            _toDestroy.Add(active);
            active.abilityName = "Ember Rush";
            active.description = "Charge forward, scorching foes in your path.";
            active.soulPowerCost = 2;

            beast.levels[1].passiveEffects = new List<PassiveEffect> { passiveLv2 };
            beast.levels.Add(new SoulBeastLevelData
            {
                passiveEffects = new List<PassiveEffect> { passiveLv3 },
                activeAbilities = new List<AbilityAction> { active },
            });
            return beast;
        }

        void CreateRegistry(SoulBeastDefinition beast)
        {
            var registry = ScriptableObject.CreateInstance<SoulBeastRegistry>();
            _toDestroy.Add(registry);
            registry.beasts = new List<SoulBeastDefinition> { beast };
            SoulBeastRegistryService.SetRegistryForTests(registry);
        }

        GameObject CreateUnbondedBeastman()
        {
            var go = new GameObject("BeastmanTest");
            _toDestroy.Add(go);
            var stats = go.AddComponent<CharacterStats>();
            stats.race = Race.Beastman;
            stats.racialSubsystem = RacialSubsystemKind.BeastmanSoulBeast;
            stats.level = 5;
            go.AddComponent<BeastmanSoulBeastRuntime>();
            return go;
        }

        GameObject CreateBondedBeastman(SoulBeastDefinition beast, int startingLevel)
        {
            GameObject go = CreateUnbondedBeastman();
            var runtime = go.GetComponent<BeastmanSoulBeastRuntime>();
            Assert.IsTrue(runtime.TryFormContract(beast, out _));
            for (int i = 1; i < startingLevel; i++)
                runtime.TryIncrementLevel(out _);
            return go;
        }
    }
}
