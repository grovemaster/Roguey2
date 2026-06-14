using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Progression.Proficiency;
using JRogue.Racial;
using JRogue.Stats;
using JRogue.Stats.Racial;
using JRogue.UI.Proficiency;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.UI.Proficiency
{
    [TestFixture]
    public sealed class ProficiencyListBodyViewModelTests
    {
        readonly List<GameObject> _created = new();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _created)
            {
                if (go != null)
                    Object.DestroyImmediate(go);
            }

            _created.Clear();
        }

        [Test]
        public void Build_Knight_FireMagicIsIneligible()
        {
            BaseActor actor = CreateActor(Race.Human, HumanClass.Knight, characterLevel: 10);
            ProficiencySheetModel sheet = ProficiencyListBodyViewModel.Build(actor);

            ProficiencyRowViewModel fireMagic = sheet.FindRow(ProficiencyKind.FireMagic);
            Assert.NotNull(fireMagic);
            Assert.IsFalse(fireMagic.Eligible);
            Assert.AreEqual("N/A", fireMagic.LevelDisplayText);
            Assert.AreEqual(
                "Only a Human Mage can train this proficiency.",
                fireMagic.IneligibilityReason);
        }

        [Test]
        public void Build_Knight_FlamingSwordTrainsFireDamageNotFireMagic()
        {
            BaseActor actor = CreateActor(Race.Human, HumanClass.Knight, characterLevel: 10);
            ProficiencyRuntime runtime = actor.GetComponent<ProficiencyRuntime>();
            runtime.SetLevelForTests(ProficiencyKind.Damage_Fire, 4);

            ProficiencySheetModel sheet = ProficiencyListBodyViewModel.Build(actor);

            Assert.AreEqual(4, sheet.FindRow(ProficiencyKind.Damage_Fire).StoredLevel);
            Assert.AreEqual(0, sheet.FindRow(ProficiencyKind.FireMagic).StoredLevel);
        }

        [Test]
        public void Build_Mage_FireMagicIsEligible()
        {
            BaseActor actor = CreateActor(Race.Human, HumanClass.Mage, characterLevel: 10);
            ProficiencySheetModel sheet = ProficiencyListBodyViewModel.Build(actor);

            ProficiencyRowViewModel fireMagic = sheet.FindRow(ProficiencyKind.FireMagic);
            Assert.IsTrue(fireMagic.Eligible);
            Assert.AreEqual("0 / 20", fireMagic.LevelDisplayText);
        }

        [Test]
        public void Build_AboveTrainingCap_ShowsCapSuffixAndWarning()
        {
            BaseActor actor = CreateActor(Race.Human, HumanClass.Knight, characterLevel: 5);
            CharacterStats stats = actor.stats;
            stats.level = 5;

            ProficiencyRuntime runtime = actor.GetComponent<ProficiencyRuntime>();
            runtime.SetLevelForTests(ProficiencyKind.Weapon_Sword, 18, pxp: 120);

            ProficiencySheetModel sheet = ProficiencyListBodyViewModel.Build(actor);
            ProficiencyRowViewModel row = sheet.FindRow(ProficiencyKind.Weapon_Sword);

            Assert.AreEqual("18 (cap 10)", row.LevelDisplayText);
            Assert.IsTrue(row.IsAboveTrainingCap);
            Assert.IsFalse(row.ShowProgressBar);
            Assert.IsFalse(string.IsNullOrEmpty(sheet.CapWarningLine));

            string detail = ProficiencyDetailFormatter.BuildBody(row);
            StringAssert.Contains("Training paused", detail);
            StringAssert.Contains("120", detail);
        }

        [Test]
        public void ResolveDefaultSelection_PrefersFirstNonZeroThenEligible()
        {
            BaseActor actor = CreateActor(Race.Human, HumanClass.Knight, characterLevel: 10);
            ProficiencyRuntime runtime = actor.GetComponent<ProficiencyRuntime>();
            runtime.SetLevelForTests(ProficiencyKind.Weapon_Sword, 7);

            ProficiencySheetModel sheet = ProficiencyListBodyViewModel.Build(actor);
            Assert.AreEqual(ProficiencyKind.Weapon_Sword, sheet.ResolveDefaultSelection());
        }

        [Test]
        public void Build_IncludesEveryCatalogKindOnce()
        {
            BaseActor actor = CreateActor(Race.Human, HumanClass.Knight, characterLevel: 10);
            ProficiencySheetModel sheet = ProficiencyListBodyViewModel.Build(actor);

            int expectedCount = 0;
            foreach (ProficiencyKind kind in System.Enum.GetValues(typeof(ProficiencyKind)))
            {
                if (kind == ProficiencyKind.None)
                    continue;

                expectedCount++;
                Assert.NotNull(sheet.FindRow(kind), $"Missing row for {kind}");
            }

            Assert.AreEqual(expectedCount, sheet.Rows.Count);
        }

        BaseActor CreateActor(Race race, HumanClass humanClass, int characterLevel)
        {
            var go = new GameObject("ProficiencyMenuTestActor");
            _created.Add(go);

            var stats = go.AddComponent<CharacterStats>();
            stats.race = race;
            stats.humanClass = humanClass;
            stats.level = characterLevel;

            go.AddComponent<ProficiencyRuntime>();
            var actor = go.AddComponent<TestActor>();
            return actor;
        }

        sealed class TestActor : BaseActor
        {
            protected override void Die() { }
        }
    }
}
