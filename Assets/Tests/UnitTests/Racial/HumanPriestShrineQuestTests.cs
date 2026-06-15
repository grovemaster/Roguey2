using JRogue.Racial;
using JRogue.Stats;
using JRogue.Stats.Racial;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.Racial
{
    [TestFixture]
    public sealed class HumanPriestShrineQuestTests
    {
        readonly System.Collections.Generic.List<GameObject> _created = new();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _created)
            {
                if (go != null)
                    Object.DestroyImmediate(go);
            }

            _created.Clear();
            PatronGodCatalogService.ResetCacheForTests();
            PriestInvocationCatalogService.ResetCacheForTests();
            HumanPriestPietyService.ResetCacheForTests();
            Object.DestroyImmediate(_patron);
            _patron = null;
        }

        PatronGodDefinition _patron;

        [Test]
        public void CanBeginPriestInitiation_RequiresHumanNoneWithoutEssences()
        {
            GameObject actor = CreateHumanActor(HumanClass.Mage);
            Assert.IsFalse(HumanPriestClassCommitService.CanBeginPriestInitiation(
                actor.GetComponent<JRogue.Actors.BaseActor>(),
                out string reason));
            Assert.That(reason, Does.Contain("committed"));
        }

        [Test]
        public void TryCommit_PriestBootstrapsCovenantAndDevotion()
        {
            PatronGodDefinition patron = ScriptableObject.CreateInstance<PatronGodDefinition>();
            patron.godId = HumanPriestShrineIds.ArgentVigilGodId;
            _patron = patron;
            PatronGodCatalogService.RegisterForTests(patron);

            var layOnHands = ScriptableObject.CreateInstance<PriestInvocationDefinition>();
            layOnHands.invocationId = "priest_lay_on_hands";
            layOnHands.displayName = "Lay on Hands";
            PriestInvocationCatalogService.RegisterForTests(layOnHands);

            var smite = ScriptableObject.CreateInstance<PriestInvocationDefinition>();
            smite.invocationId = "priest_smites_undead";
            smite.displayName = "Smite Undead";
            smite.requiredPiety = 10;
            PriestInvocationCatalogService.RegisterForTests(smite);

            GameObject actor = CreateHumanActor(HumanClass.None);
            Assert.IsTrue(HumanClassCommitment.TryCommit(actor, HumanClass.Priest, out string error), error);

            Assert.AreEqual(HumanClass.Priest, actor.GetComponent<CharacterStats>().humanClass);
            HumanPriestCovenantRuntime covenant = actor.GetComponent<HumanPriestCovenantRuntime>();
            HumanPriestDevotionRuntime devotion = actor.GetComponent<HumanPriestDevotionRuntime>();

            Assert.IsNotNull(covenant);
            Assert.IsNotNull(devotion);
            Assert.AreEqual(HumanPriestShrineIds.ArgentVigilGodId, covenant.PatronGodId);
            Assert.GreaterOrEqual(covenant.Piety, 10);
            Assert.GreaterOrEqual(devotion.EquippedInvocations.Count, 1);
        }

        [Test]
        public void ResolveDevotionSlotCap_FallsBackWithoutProgressionAsset()
        {
            Assert.AreEqual(2, HumanPriestPietyService.ResolveDevotionSlotCap(0));
            Assert.AreEqual(3, HumanPriestPietyService.ResolveDevotionSlotCap(20));
        }

        [Test]
        public void IsInvocationBlockedByPenance_BlocksHighTierOnly()
        {
            var covenant = new GameObject("PenanceTest").AddComponent<HumanPriestCovenantRuntime>();
            _created.Add(covenant.gameObject);
            covenant.InitializeOnCommit("argent_vigil", 20);

            var lowTier = ScriptableObject.CreateInstance<PriestInvocationDefinition>();
            lowTier.invocationId = "priest_ward";
            lowTier.requiredPiety = 0;

            var highTier = ScriptableObject.CreateInstance<PriestInvocationDefinition>();
            highTier.invocationId = "priest_sanctuary";
            highTier.requiredPiety = 20;

            Assert.IsFalse(PriestPietyLogic.IsInvocationBlockedByPenance(covenant, lowTier));

            covenant.AddPenance(5, "test");
            Assert.IsFalse(PriestPietyLogic.IsInvocationBlockedByPenance(covenant, lowTier));
            Assert.IsTrue(PriestPietyLogic.IsInvocationBlockedByPenance(covenant, highTier));
        }

        [Test]
        public void MeetsCompletionGates_AllowsReportWhenDungeonInactive()
        {
            var vow = ScriptableObject.CreateInstance<PriestVowDefinition>();
            vow.minFloorIndex = 2;
            vow.minDayNightInDungeon = 2;
            Assert.IsTrue(HumanPriestVowLogic.MeetsCompletionGates(vow, out string reason), reason);
        }

        GameObject CreateHumanActor(HumanClass humanClass)
        {
            var go = new GameObject("HumanPriestQuestTest");
            _created.Add(go);
            go.AddComponent<JRogue.Actors.BaseActor>();
            var stats = go.AddComponent<CharacterStats>();
            stats.race = Race.Human;
            stats.humanClass = humanClass;
            stats.level = 1;
            stats.RefreshResourcePoolsToMax();
            return go;
        }
    }
}
