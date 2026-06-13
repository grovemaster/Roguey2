using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Progression.Proficiency;
using JRogue.Racial;
using JRogue.Stats;
using JRogue.Stats.Racial;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.Progression
{
    [TestFixture]
    public sealed class ProficiencySystemTests
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
        public void GetTrainingCap_ScalesWithCharacterLevel()
        {
            Assert.AreEqual(2, ProficiencyRules.GetTrainingCap(1));
            Assert.AreEqual(20, ProficiencyRules.GetTrainingCap(10));
            Assert.AreEqual(27, ProficiencyRules.GetTrainingCap(14));
        }

        [Test]
        public void AddPxp_StopsAtTrainingCapAndBanksProgress()
        {
            GameObject go = CreateActor(Race.Human, HumanClass.Knight, characterLevel: 5);
            ProficiencyRuntime runtime = go.GetComponent<ProficiencyRuntime>();
            CharacterStats stats = go.GetComponent<CharacterStats>();

            runtime.AddPxp(stats, ProficiencyKind.Weapon_Sword, 5000);

            Assert.AreEqual(10, runtime.GetLevel(ProficiencyKind.Weapon_Sword));
            Assert.Greater(runtime.GetPxp(ProficiencyKind.Weapon_Sword), 0);
        }

        [Test]
        public void AddPxp_DoesNotReduceStoredLevelWhenCharacterLevelWouldCapLower()
        {
            GameObject go = CreateActor(Race.Human, HumanClass.Knight, characterLevel: 10);
            ProficiencyRuntime runtime = go.GetComponent<ProficiencyRuntime>();
            CharacterStats stats = go.GetComponent<CharacterStats>();

            runtime.SetLevelForTests(ProficiencyKind.Weapon_Sword, 18);
            stats.level = 5;

            runtime.AddPxp(stats, ProficiencyKind.Weapon_Sword, 500);
            Assert.AreEqual(18, runtime.GetLevel(ProficiencyKind.Weapon_Sword));
        }

        [Test]
        public void BuildAwards_FlamingSwordIncludesFireAndSlash()
        {
            var action = new ProficiencyResolvedAction
            {
                HasWeaponType = true,
                WeaponType = WeaponType.Sword,
                CountsAsWeaponHit = true,
                DamageModulesApplied = new List<JRogue.Item.DamageEntry>
                {
                    new() { type = DamageType.Slash, value = 8 },
                    new() { type = DamageType.Fire, value = 3 },
                },
            };

            IReadOnlyList<ProficiencyAward> awards = ProficiencyXpDispatcher.BuildAwards(action);

            AssertAwardContains(awards, ProficiencyKind.Weapon_Sword, 12);
            AssertAwardContains(awards, ProficiencyKind.Damage_Slash, 12);
            AssertAwardContains(awards, ProficiencyKind.Damage_Fire, 12);
            AssertAwardContains(awards, ProficiencyKind.Fighting, 6);
        }

        [Test]
        public void Dispatch_KnightFlamingSword_DoesNotTrainFireMagic()
        {
            GameObject go = CreateActor(Race.Human, HumanClass.Knight, characterLevel: 10);
            BaseActor actor = go.GetComponent<BaseActor>();
            ProficiencyRuntime runtime = go.GetComponent<ProficiencyRuntime>();

            var action = new ProficiencyResolvedAction
            {
                HasWeaponType = true,
                WeaponType = WeaponType.Sword,
                CountsAsWeaponHit = true,
                DamageModulesApplied = new List<JRogue.Item.DamageEntry>
                {
                    new() { type = DamageType.Slash, value = 8 },
                    new() { type = DamageType.Fire, value = 3 },
                },
            };

            ProficiencyXpDispatcher.Dispatch(actor, action);

            Assert.Greater(runtime.GetLevel(ProficiencyKind.Damage_Fire), 0);
            Assert.AreEqual(0, runtime.GetLevel(ProficiencyKind.FireMagic));
        }

        [Test]
        public void BuildAwards_MageFireball_IncludesSchoolAtFullAndDamageAtHalf()
        {
            var spell = ScriptableObject.CreateInstance<MageSpellDefinition>();
            spell.magicPowerCost = 5;
            spell.proficiencyTags = new List<ProficiencyKind>
            {
                ProficiencyKind.Spellcasting,
                ProficiencyKind.FireMagic,
            };

            ProficiencyResolvedAction action =
                ProficiencyStrikePayloadBuilder.FromMageSpellCast(spell, null);

            IReadOnlyList<ProficiencyAward> awards = ProficiencyXpDispatcher.BuildAwards(action);

            AssertAwardContains(awards, ProficiencyKind.Spellcasting, 15);
            AssertAwardContains(awards, ProficiencyKind.FireMagic, 15);
            AssertAwardContains(awards, ProficiencyKind.Damage_Fire, 8);

            Object.DestroyImmediate(spell);
        }

        static void AssertAwardContains(
            IReadOnlyList<ProficiencyAward> awards,
            ProficiencyKind kind,
            int expectedPxp)
        {
            for (int i = 0; i < awards.Count; i++)
            {
                if (awards[i].Kind == kind)
                {
                    Assert.AreEqual(expectedPxp, awards[i].Pxp);
                    return;
                }
            }

            Assert.Fail($"Missing award for {kind}");
        }

        GameObject CreateActor(Race race, HumanClass humanClass, int characterLevel)
        {
            var go = new GameObject("ProficiencyTestActor");
            _created.Add(go);

            var stats = go.AddComponent<CharacterStats>();
            stats.race = race;
            stats.humanClass = humanClass;
            stats.level = characterLevel;

            go.AddComponent<ProficiencyRuntime>();
            go.AddComponent<TestProficiencyActor>();
            return go;
        }

        sealed class TestProficiencyActor : BaseActor
        {
            protected override void Die() { }
        }
    }
}
