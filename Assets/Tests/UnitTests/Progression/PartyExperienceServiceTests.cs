using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Actors.Components;
using JRogue.Controller.Enemy;
using JRogue.Data.Enemy;
using JRogue.Data.Progression;
using JRogue.Manager.Party;
using JRogue.Manager.Progression;
using JRogue.Stats;
using JRogue.Tests.UnitTests.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static JRogue.Tests.UnitTests.Input.InputTestSceneBuilder;

namespace JRogue.Tests.UnitTests.Progression
{
    [TestFixture]
    public class PartyExperienceServiceTests
    {
        readonly List<GameObject> _created = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            InputTestSceneBuilder.ResetSingletonManagersForTests();
            LogAssert.ignoreFailingMessages = true;
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            ClearPartyExperienceServiceInstance();
            foreach (GameObject go in _created)
            {
                if (go != null)
                    Object.DestroyImmediate(go);
            }

            _created.Clear();
            InputTestSceneBuilder.ResetSingletonManagersForTests();
        }

        [Test]
        public void FirstKillAwardsXpToAllMembers_RepeatKillAwardsZero()
        {
            PartyExperienceService svc = CreateService(out PartyManager party, out ExperienceCurve curve);
            EnemySpeciesDefinition skeleton = CreateSpecies("skeleton", "Skeleton", 25);

            EnemyController first = SpawnEnemy(skeleton);
            KillEnemy(svc, first, party.partyMembers[0].gameObject);
            Assert.AreEqual(25, party.partyMembers[0].stats.experience);
            Assert.AreEqual(25, party.partyMembers[1].stats.experience);

            EnemyController second = SpawnEnemy(skeleton);
            KillEnemy(svc, second, party.partyMembers[0].gameObject);
            Assert.AreEqual(25, party.partyMembers[0].stats.experience);
        }

        [Test]
        public void GiantSkeletonIsIndependentSpeciesJournalEntry()
        {
            PartyExperienceService svc = CreateService(out PartyManager party, out _);
            EnemySpeciesDefinition skeleton = CreateSpecies("skeleton", "Skeleton", 25);
            EnemySpeciesDefinition giant = CreateSpecies("giant_skeleton", "Giant Skeleton", 100);

            KillEnemy(svc, SpawnEnemy(skeleton), party.partyMembers[0].gameObject);
            KillEnemy(svc, SpawnEnemy(giant), party.partyMembers[0].gameObject);

            Assert.AreEqual(25, party.partyMembers[0].stats.experience);
            Assert.AreEqual(125, party.partyMembers[0].stats.experience);
        }

        [Test]
        public void LevelUpIncreasesConstitutionMaxHpAndSoulPower()
        {
            ExperienceCurve curve = ScriptableObject.CreateInstance<ExperienceCurve>();
            curve.baseXpPerLevel = 10;
            curve.constitutionPerLevel = 1;
            curve.maxSoulPowerPerLevel = 2;

            PartyExperienceService svc = CreateService(curve, 1, out PartyManager party);
            CharacterStats stats = party.partyMembers[0].stats;

            int conBefore = stats.Constitution.GetValue();
            int maxHpBefore = stats.MaxHP;
            int maxSoulBefore = stats.MaxSoulPower;

            svc.ApplyExperienceGain(stats, 10, "test");

            Assert.AreEqual(2, stats.level);
            Assert.Greater(stats.Constitution.GetValue(), conBefore);
            Assert.Greater(stats.MaxHP, maxHpBefore);
            Assert.Greater(stats.MaxSoulPower, maxSoulBefore);
            Assert.Greater(stats.currentHP, maxHpBefore);
        }

        [Test]
        public void MaxLevel50DoesNotExceedCap()
        {
            ExperienceCurve curve = ScriptableObject.CreateInstance<ExperienceCurve>();
            curve.baseXpPerLevel = 1;

            PartyExperienceService svc = CreateService(curve, 1, out PartyManager party);
            CharacterStats stats = party.partyMembers[0].stats;
            stats.level = 50;

            svc.ApplyExperienceGain(stats, 999, "test");
            Assert.AreEqual(50, stats.level);
            Assert.Greater(stats.experience, 0);
        }

        [Test]
        public void JournalTryRegisterFirstKill_IsIdempotent()
        {
            var journal = new PartySpeciesJournal();
            Assert.IsTrue(journal.TryRegisterFirstKill("skeleton"));
            Assert.IsTrue(journal.HasDefeated("skeleton"));
            Assert.IsFalse(journal.TryRegisterFirstKill("skeleton"));
        }

        static void KillEnemy(PartyExperienceService svc, EnemyController enemy, GameObject killer)
        {
            svc.HandleEnemyDeath(enemy, killer);
            Object.DestroyImmediate(enemy.gameObject);
        }

        EnemyController SpawnEnemy(EnemySpeciesDefinition species)
        {
            GameObject go = new GameObject("TestEnemy");
            _created.Add(go);
            go.AddComponent<GridMover>();
            go.AddComponent<CharacterStats>();
            go.AddComponent<HealthComponent>();
            EnemyController enemy = go.AddComponent<EnemyController>();
            SetPrivateSpecies(enemy, species);
            return enemy;
        }

        static void SetPrivateSpecies(EnemyController enemy, EnemySpeciesDefinition species)
        {
            var field = typeof(EnemyController).GetField(
                "species",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(enemy, species);
        }

        PartyExperienceService CreateService(out PartyManager party, out ExperienceCurve curve)
        {
            curve = ScriptableObject.CreateInstance<ExperienceCurve>();
            curve.baseXpPerLevel = 100;
            return CreateService(curve, 2, out party);
        }

        PartyExperienceService CreateService(ExperienceCurve curve, int partySize, out PartyManager party)
        {
            party = CreatePartyWithTestActors(partySize, _created);
            PartyExperienceService svc = party.GetComponent<PartyExperienceService>();
            if (svc == null)
                svc = party.gameObject.AddComponent<PartyExperienceService>();
            SetPrivateField(svc, "experienceCurve", curve);
            return svc;
        }

        static void ClearPartyExperienceServiceInstance()
        {
            var prop = typeof(PartyExperienceService).GetProperty(
                "Instance",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            prop?.GetSetMethod(true)?.Invoke(null, new object[] { null });
        }

        static EnemySpeciesDefinition CreateSpecies(string id, string display, int xp)
        {
            var def = ScriptableObject.CreateInstance<EnemySpeciesDefinition>();
            def.speciesId = id;
            def.displayName = display;
            def.firstKillExperience = xp;
            return def;
        }

        static void SetPrivateField(object target, string name, object value)
        {
            var field = target.GetType().GetField(
                name,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(target, value);
        }
    }
}
