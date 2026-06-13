using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Racial;
using JRogue.Stats;
using JRogue.Stats.Racial;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.Racial
{
    [TestFixture]
    public sealed class ElementalSpiritMeditationTests
    {
        readonly List<Object> _destroy = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            ElementalSpiritProgressionConfig.ResetForTests();
            ElementalSpiritProgressionLogic.ResetCapPolicyForTests();

            foreach (Object o in _destroy)
            {
                if (o != null)
                    Object.DestroyImmediate(o);
            }

            _destroy.Clear();
        }

        [Test]
        public void AwardExperience_LevelsUp_WhenThresholdMet()
        {
            ElementalSpiritLevelCurve curve = CreateCurve(10, 20);
            ElementalSpiritProgressionConfig config = CreateProgressionConfig(curve);
            _destroy.Add(config);
            ElementalSpiritProgressionConfig.SetForTests(config);
            BaseActor elf = CreateElfWithSpirit(out ElementalSpiritContractsRuntime runtime, out string instanceId);
            elf.stats.level = 5;

            Assert.IsTrue(ElementalSpiritMeditationLogic.TryAwardSpiritExperience(
                elf,
                runtime,
                instanceId,
                10,
                "test",
                out ElementalSpiritMeditationAwardResult result,
                out _));

            Assert.AreEqual(1, result.LevelsGained);
            Assert.AreEqual(2, result.FinalContractLevel);
            Assert.AreEqual(0, result.FinalContractExperience);
        }

        [Test]
        public void AwardExperience_BlockedAtCap()
        {
            ElementalSpiritLevelCurve curve = CreateCurve(10, 20);
            ElementalSpiritProgressionConfig config = CreateProgressionConfig(curve);
            _destroy.Add(config);
            ElementalSpiritProgressionConfig.SetForTests(config);
            BaseActor elf = CreateElfWithSpirit(out ElementalSpiritContractsRuntime runtime, out string instanceId);
            elf.stats.level = 1;

            Assert.IsFalse(ElementalSpiritMeditationLogic.TryAwardSpiritExperience(
                elf,
                runtime,
                instanceId,
                10,
                "test",
                out _,
                out string failure));

            Assert.IsNotEmpty(failure);
        }

        [Test]
        public void AwardExperience_MultiLevel_WithOverflow()
        {
            ElementalSpiritLevelCurve curve = CreateCurve(10, 20);
            ElementalSpiritProgressionConfig config = CreateProgressionConfig(curve);
            _destroy.Add(config);
            ElementalSpiritProgressionConfig.SetForTests(config);
            BaseActor elf = CreateElfWithSpirit(out ElementalSpiritContractsRuntime runtime, out string instanceId);
            elf.stats.level = 5;

            Assert.IsTrue(ElementalSpiritMeditationLogic.TryAwardSpiritExperience(
                elf,
                runtime,
                instanceId,
                35,
                "test",
                out ElementalSpiritMeditationAwardResult result,
                out _));

            Assert.AreEqual(2, result.LevelsGained);
            Assert.AreEqual(3, result.FinalContractLevel);
            Assert.AreEqual(5, result.FinalContractExperience);
        }

        [Test]
        public void ContractLevel_Unchanged_WhenElfLevelDropsBelowContractLevel()
        {
            ElementalSpiritLevelCurve curve = CreateCurve(10, 20, 30);
            ElementalSpiritProgressionConfig config = CreateProgressionConfig(curve);
            _destroy.Add(config);
            ElementalSpiritProgressionConfig.SetForTests(config);
            BaseActor elf = CreateElfWithSpirit(out ElementalSpiritContractsRuntime runtime, out string instanceId);
            elf.stats.level = 5;

            runtime.TryGetPreset(instanceId, out ElementalSpiritContractPreset preset);
            preset.contractLevel = 4;
            preset.contractExperience = 0;
            elf.stats.level = 2;

            Assert.IsTrue(ElementalSpiritProgressionLogic.IsCappedForXpGain(elf, preset));
            Assert.AreEqual(4, preset.contractLevel);
        }

        [Test]
        public void TryFormContract_StartsWithZeroExperience()
        {
            ElementalSpiritDefinition spirit = BuildSpirit();
            BaseActor elf = CreateBareElf();
            var runtime = elf.GetComponent<ElementalSpiritContractsRuntime>();

            Assert.IsTrue(runtime.TryFormContract(spirit, 1, out _, out _));
            Assert.AreEqual(0, runtime.ContractedSpirits[0].contractExperience);
        }

        BaseActor CreateElfWithSpirit(
            out ElementalSpiritContractsRuntime runtime,
            out string instanceId)
        {
            ElementalSpiritDefinition spirit = BuildSpirit();
            BaseActor elf = CreateBareElf();
            runtime = elf.GetComponent<ElementalSpiritContractsRuntime>();
            Assert.IsTrue(runtime.TryFormContract(spirit, 1, out instanceId, out _));
            return elf;
        }

        BaseActor CreateBareElf()
        {
            var go = new GameObject("ElfMeditationTest");
            _destroy.Add(go);
            var stats = go.AddComponent<CharacterStats>();
            stats.race = Race.Elf;
            stats.racialSubsystem = RacialSubsystemKind.ElfElementalContracts;
            stats.level = 3;
            go.AddComponent<ElementalSpiritContractsRuntime>();
            return go.AddComponent<BaseActor>();
        }

        ElementalSpiritDefinition BuildSpirit()
        {
            var spirit = ScriptableObject.CreateInstance<ElementalSpiritDefinition>();
            spirit.spiritId = "test_spirit";
            spirit.displayName = "Test Spirit";
            spirit.maxLevel = 3;
            spirit.levels = new List<ElementalSpiritLevelData>
            {
                new ElementalSpiritLevelData(),
                new ElementalSpiritLevelData(),
                new ElementalSpiritLevelData(),
            };
            _destroy.Add(spirit);
            return spirit;
        }

        ElementalSpiritLevelCurve CreateCurve(params int[] thresholds)
        {
            var curve = ScriptableObject.CreateInstance<ElementalSpiritLevelCurve>();
            curve.xpToReachNextLevel = new List<int>(thresholds);
            _destroy.Add(curve);
            return curve;
        }

        static ElementalSpiritProgressionConfig CreateProgressionConfig(ElementalSpiritLevelCurve curve)
        {
            var config = ScriptableObject.CreateInstance<ElementalSpiritProgressionConfig>();
            config.defaultLevelCurve = curve;
            return config;
        }
    }
}
