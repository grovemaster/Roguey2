using System.Collections.Generic;
using JRogue.Item;
using JRogue.Item.Essence;
using JRogue.Racial;
using JRogue.Stats;
using JRogue.Stats.Racial;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.Racial
{
    public class SoulBeastRitualLogicTests
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
            SoulBeastRegistryService.ResetRegistryForTests();
        }

        [Test]
        public void BuildPool_EnhancementRite_ExcludesSummoningBeast()
        {
            SoulBeastRegistry registry = CreateRegistry(
                CreateBeast("ember_wolf", SoulBeastType.Enhancement),
                CreateBeast("storm_hawk", SoulBeastType.Summoning));
            SoulBeastRitualTypeDefinition ritualType = CreateRitualType(SoulBeastType.Enhancement);

            List<SoulBeastWeightedCandidate> pool =
                SoulBeastRitualLogic.BuildWeightedPool(registry, ritualType, null);

            Assert.AreEqual(1, pool.Count);
            Assert.AreEqual("ember_wolf", pool[0].Beast.soulBeastId);
        }

        [Test]
        public void BuildPool_OfferingTagFilter_ExcludesNonMatchingBeasts()
        {
            SoulBeastDefinition wolf = CreateBeast("ember_wolf", SoulBeastType.Enhancement);
            wolf.tags = new List<string> { "wolf" };
            SoulBeastDefinition tortoise = CreateBeast("stone_tortoise", SoulBeastType.Enhancement);
            tortoise.tags = new List<string> { "tortoise" };

            SoulBeastRegistry registry = CreateRegistry(wolf, tortoise);
            SoulBeastRitualTypeDefinition ritualType = CreateRitualType(SoulBeastType.Enhancement);
            RitualOfferingItemData offeringItem = CreateOfferingItem(new List<string> { "wolf" });

            List<SoulBeastWeightedCandidate> pool = SoulBeastRitualLogic.BuildWeightedPool(
                registry,
                ritualType,
                new ItemData[] { offeringItem });

            Assert.AreEqual(1, pool.Count);
            Assert.AreEqual("ember_wolf", pool[0].Beast.soulBeastId);
        }

        [Test]
        public void RollAppearance_NoneOutcomeWeight_CanFail()
        {
            SoulBeastDefinition wolf = CreateBeast("ember_wolf", SoulBeastType.Enhancement);
            var pool = new List<SoulBeastWeightedCandidate>
            {
                new SoulBeastWeightedCandidate(wolf, 1),
            };

            var rng = new System.Random(0);
            SoulBeastDefinition result = SoulBeastRitualLogic.RollAppearance(pool, noneOutcomeWeight: 100, rng);
            Assert.IsNull(result);
        }

        [Test]
        public void RollAppearance_WithSeed_SelectsBeast()
        {
            SoulBeastDefinition wolf = CreateBeast("ember_wolf", SoulBeastType.Enhancement);
            var pool = new List<SoulBeastWeightedCandidate>
            {
                new SoulBeastWeightedCandidate(wolf, 10),
            };

            var rng = new System.Random(12345);
            SoulBeastDefinition result = SoulBeastRitualLogic.RollAppearance(pool, noneOutcomeWeight: 0, rng);
            Assert.AreEqual("ember_wolf", result.soulBeastId);
        }

        SoulBeastRegistry CreateRegistry(params SoulBeastDefinition[] beasts)
        {
            var registry = ScriptableObject.CreateInstance<SoulBeastRegistry>();
            _destroy.Add(registry);
            registry.beasts = new List<SoulBeastDefinition>(beasts);
            foreach (SoulBeastDefinition beast in beasts)
                _destroy.Add(beast);

            SoulBeastRegistryService.SetRegistryForTests(registry);
            return registry;
        }

        static SoulBeastDefinition CreateBeast(string id, SoulBeastType type)
        {
            var beast = ScriptableObject.CreateInstance<SoulBeastDefinition>();
            beast.soulBeastId = id;
            beast.displayName = id;
            beast.soulBeastType = type;
            beast.maxLevel = 3;
            beast.levels = new List<SoulBeastLevelData>
            {
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

        static SoulBeastRitualTypeDefinition CreateRitualType(SoulBeastType type)
        {
            var ritualType = ScriptableObject.CreateInstance<SoulBeastRitualTypeDefinition>();
            ritualType.ritualTypeId = $"ritual_{type}";
            ritualType.allowedSoulBeastTypes = new List<SoulBeastType> { type };
            ritualType.noneOutcomeWeight = 50;
            return ritualType;
        }

        RitualOfferingItemData CreateOfferingItem(List<string> tags)
        {
            var offeringDef = ScriptableObject.CreateInstance<SoulBeastRitualOfferingDefinition>();
            _destroy.Add(offeringDef);
            offeringDef.poolFilterTags = tags;

            var item = ScriptableObject.CreateInstance<RitualOfferingItemData>();
            _destroy.Add(item);
            item.ritualOffering = offeringDef;
            return item;
        }
    }

    public class SoulBeastLevelLogicTests
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
            SoulBeastRegistryService.ResetRegistryForTests();
            SoulBeastProgressionLogic.CapPolicy = CharacterLevelSoulBeastCapPolicy.Instance;
        }

        [Test]
        public void EffectiveCap_UsesContractorLevel()
        {
            SoulBeastDefinition beast = CreateBeastWithLevels();
            var statsGo = new GameObject("Stats");
            _destroy.Add(statsGo);
            var stats = statsGo.AddComponent<CharacterStats>();
            stats.level = 3;

            int cap = SoulBeastProgressionLogic.GetEffectiveLevelCap(stats, beast);
            Assert.AreEqual(3, cap);
        }

        [Test]
        public void DoubleCapPolicy_AllowsTwiceContractorLevel()
        {
            SoulBeastProgressionLogic.CapPolicy = DoubleCharacterLevelSoulBeastCapPolicy.Instance;
            SoulBeastDefinition beast = CreateBeastWithLevels();
            var statsGo = new GameObject("Stats");
            _destroy.Add(statsGo);
            var stats = statsGo.AddComponent<CharacterStats>();
            stats.level = 3;

            int cap = SoulBeastProgressionLogic.GetEffectiveLevelCap(stats, beast);
            Assert.AreEqual(5, cap);
        }

        [Test]
        public void Runtime_TryIncrementLevel_RaisesLevelAndStats()
        {
            SoulBeastDefinition beast = CreateBeastWithLevels();
            CreateRegistry(beast);
            GameObject go = CreateBondedBeastman(beast, startingLevel: 1);
            CharacterStats stats = go.GetComponent<CharacterStats>();
            stats.level = 5;
            var runtime = go.GetComponent<BeastmanSoulBeastRuntime>();

            Assert.AreEqual(11, stats.Strength.GetValue());
            Assert.IsTrue(runtime.TryIncrementLevel(out _));
            Assert.AreEqual(2, runtime.SoulBeastLevel);
            Assert.AreEqual(12, stats.Strength.GetValue());
        }

        [Test]
        public void Runtime_AtCap_BlocksIncrement()
        {
            SoulBeastDefinition beast = CreateBeastWithLevels();
            CreateRegistry(beast);
            GameObject go = CreateBondedBeastman(beast, startingLevel: 1);
            CharacterStats stats = go.GetComponent<CharacterStats>();
            stats.level = 1;
            var runtime = go.GetComponent<BeastmanSoulBeastRuntime>();

            Assert.IsFalse(runtime.TryIncrementLevel(out string reason));
            StringAssert.Contains("cannot exceed", reason);
        }

        [Test]
        public void Runtime_TryFormContract_SetsLevelOne()
        {
            SoulBeastDefinition beast = CreateBeastWithLevels();
            CreateRegistry(beast);
            GameObject go = CreateUnbondedBeastman();
            var runtime = go.GetComponent<BeastmanSoulBeastRuntime>();

            Assert.IsTrue(runtime.TryFormContract(beast, out _));
            Assert.AreEqual("ember_wolf", runtime.SoulBeastId);
            Assert.AreEqual(1, runtime.SoulBeastLevel);
        }

        SoulBeastDefinition CreateBeastWithLevels()
        {
            var beast = ScriptableObject.CreateInstance<SoulBeastDefinition>();
            _destroy.Add(beast);
            beast.soulBeastId = "ember_wolf";
            beast.displayName = "Ember Wolf";
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

        void CreateRegistry(SoulBeastDefinition beast)
        {
            var registry = ScriptableObject.CreateInstance<SoulBeastRegistry>();
            _destroy.Add(registry);
            registry.beasts = new List<SoulBeastDefinition> { beast };
            SoulBeastRegistryService.SetRegistryForTests(registry);
        }

        GameObject CreateUnbondedBeastman()
        {
            var go = new GameObject("BeastmanTest");
            _destroy.Add(go);
            var stats = go.AddComponent<CharacterStats>();
            stats.race = Race.Beastman;
            stats.racialSubsystem = RacialSubsystemKind.BeastmanSoulBeast;
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
