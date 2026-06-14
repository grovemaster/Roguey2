using JRogue.Actors;
using JRogue.Racial;
using JRogue.Stats;
using JRogue.Stats.Racial;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.Racial
{
    [TestFixture]
    public sealed class HumanKnightDrillMasterQuestTests
    {
        readonly System.Collections.Generic.List<GameObject> _created = new System.Collections.Generic.List<GameObject>();

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
        public void CanBeginKnightTraining_RequiresHumanNone()
        {
            GameObject actor = CreateHumanActor(HumanClass.Mage);
            Assert.IsFalse(HumanKnightClassCommitService.CanBeginKnightTraining(
                actor.GetComponent<BaseActor>(),
                out string reason));
            Assert.That(reason, Does.Contain("committed"));
        }

        [Test]
        public void TryCommit_KnightBootstrapsMasteryAndAuraRuntimes()
        {
            GameObject actor = CreateHumanActor(HumanClass.None);
            var tree = actor.AddComponent<HumanClassSkillTreeRuntime>();

            Assert.IsTrue(HumanClassCommitment.TryCommit(actor, HumanClass.Knight, out string error), error);
            Assert.AreEqual(HumanClass.Knight, actor.GetComponent<CharacterStats>().humanClass);
            Assert.IsNotNull(actor.GetComponent<KnightSkillMasteryRuntime>());
            Assert.IsNotNull(actor.GetComponent<KnightAuraStateRuntime>());
            Assert.IsNotNull(tree);
        }

        GameObject CreateHumanActor(HumanClass humanClass)
        {
            var go = new GameObject("HumanKnightQuestTest");
            _created.Add(go);
            go.AddComponent<BaseActor>();
            var stats = go.AddComponent<CharacterStats>();
            stats.race = Race.Human;
            stats.humanClass = humanClass;
            return go;
        }
    }
}
