using JRogue.Stats;
using JRogue.Stats.Racial;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.Racial
{
    public class Phase0RacialContractTests
    {
        [Test]
        public void Race_HasStableNumericValues()
        {
            Assert.AreEqual(0, (byte)Race.Unset);
            Assert.AreEqual(1, (byte)Race.Human);
            Assert.AreEqual(2, (byte)Race.Elf);
            Assert.AreEqual(3, (byte)Race.Barbarian);
            Assert.AreEqual(4, (byte)Race.Dwarf);
            Assert.AreEqual(5, (byte)Race.Beastman);
            Assert.AreEqual(6, (byte)Race.Dragonian);
            Assert.AreEqual(7, (byte)Race.Tiefling);
            Assert.AreEqual(8, (byte)Race.Undead);
            Assert.AreEqual(9, (byte)Race.Fairy);
        }

        [Test]
        public void HumanClass_HasStableNumericValues()
        {
            Assert.AreEqual(0, (byte)HumanClass.None);
            Assert.AreEqual(1, (byte)HumanClass.Knight);
            Assert.AreEqual(2, (byte)HumanClass.Mage);
            Assert.AreEqual(3, (byte)HumanClass.Priest);
        }

        [Test]
        public void RacialSubsystemCatalog_MapsCommitmentPolicy()
        {
            Assert.AreEqual(RacialCommitmentPolicy.Permanent,
                RacialSubsystemCatalog.GetCommitmentPolicy(RacialSubsystemKind.SpiritImprintBarbarian));
            Assert.AreEqual(RacialCommitmentPolicy.RespecAllowed,
                RacialSubsystemCatalog.GetCommitmentPolicy(RacialSubsystemKind.TieflingImplants));
            Assert.AreEqual(RacialCommitmentPolicy.RespecAllowed,
                RacialSubsystemCatalog.GetCommitmentPolicy(RacialSubsystemKind.UndeadSkillTree));
            Assert.AreEqual(RacialCommitmentPolicy.NotApplicable,
                RacialSubsystemCatalog.GetCommitmentPolicy(RacialSubsystemKind.None));
        }

        [Test]
        public void Stat_CrossSourceModifiersStack()
        {
            var stat = new Stat(10);
            var racialSource = new object();
            var essenceSource = new object();

            stat.AddModifier(5, racialSource, ModifierSourceLayer.RacialLoadout);
            stat.AddModifier(3, essenceSource, ModifierSourceLayer.Essence);

            Assert.AreEqual(18, stat.GetValue());

            stat.RemoveModifiersFromSource(racialSource);
            Assert.AreEqual(13, stat.GetValue());
        }

        [Test]
        public void RacialIdentitySnapshot_RoundTripsThroughCharacterStats()
        {
            var go = new GameObject("IdentityTest");
            try
            {
                var stats = go.AddComponent<CharacterStats>();
                stats.race = Race.Tiefling;
                stats.humanClass = HumanClass.None;
                stats.racialSubsystem = RacialSubsystemKind.TieflingImplants;
                stats.bodyCapabilities = BodyCapabilityFlags.Horns;

                var snapshot = stats.GetRacialIdentitySnapshot();
                Assert.AreEqual(RacialStackingContract.CurrentIdentitySnapshotVersion, snapshot.snapshotVersion);
                Assert.AreEqual(RacialCommitmentPolicy.RespecAllowed, snapshot.CommitmentPolicy);

                stats.race = Race.Human;
                stats.racialSubsystem = RacialSubsystemKind.None;
                stats.bodyCapabilities = BodyCapabilityFlags.None;

                Assert.IsTrue(stats.TryApplyRacialIdentitySnapshot(snapshot, out string error), error);
                Assert.AreEqual(Race.Tiefling, stats.race);
                Assert.AreEqual(RacialSubsystemKind.TieflingImplants, stats.racialSubsystem);
                Assert.AreEqual(BodyCapabilityFlags.Horns, stats.bodyCapabilities);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RacialIdentityRules_RejectsHumanClassOnNonHuman()
        {
            var snapshot = new RacialIdentitySnapshot
            {
                snapshotVersion = RacialStackingContract.CurrentIdentitySnapshotVersion,
                race = Race.Elf,
                humanClass = HumanClass.Knight,
                subsystemKind = RacialSubsystemKind.ElfElementalContracts
            };

            Assert.IsFalse(RacialIdentityRules.TryValidate(snapshot, out string error));
            Assert.That(error, Does.Contain("humanClass"));
        }

        [Test]
        public void RacialIdentityRules_RequiresHumanSpecializationWhenClassCommitted()
        {
            var snapshot = new RacialIdentitySnapshot
            {
                snapshotVersion = RacialStackingContract.CurrentIdentitySnapshotVersion,
                race = Race.Human,
                humanClass = HumanClass.Mage,
                subsystemKind = RacialSubsystemKind.None
            };

            Assert.IsFalse(RacialIdentityRules.TryValidate(snapshot, out string error));
            Assert.That(error, Does.Contain("HumanSpecialization"));
        }

        [Test]
        public void ModifierEvaluationOrder_MatchesPhase0Contract()
        {
            Assert.AreEqual(7, RacialStackingContract.ModifierEvaluationOrder.Length);
            Assert.AreEqual(ModifierSourceLayer.Base, RacialStackingContract.ModifierEvaluationOrder[0]);
            Assert.AreEqual(ModifierSourceLayer.PermanentConsumable, RacialStackingContract.ModifierEvaluationOrder[3]);
            Assert.AreEqual(ModifierSourceLayer.Temporary,
                RacialStackingContract.ModifierEvaluationOrder[RacialStackingContract.ModifierEvaluationOrder.Length - 1]);
        }
    }
}
