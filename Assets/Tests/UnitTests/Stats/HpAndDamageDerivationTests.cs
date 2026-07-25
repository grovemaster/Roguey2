using JRogue.Stats;
using JRogue.Stats.Racial;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.Stats
{
    [TestFixture]
    public class HpDerivationLogicTests
    {
        [Test]
        public void HumanL1_LandsInTutorialBand_NotConTimesTen()
        {
            int maxHp = HpDerivationLogic.ComputeMaxHp(
                raceBaseHp: 12,
                classBaseHp: 0,
                levelHpGain: 0,
                constitution: 10);
            Assert.AreEqual(22, maxHp);
            Assert.AreNotEqual(100, maxHp);
        }

        [Test]
        public void ConstitutionContribution_C1_PlusOnePerCon()
        {
            Assert.AreEqual(10, HpDerivationLogic.ConstitutionContribution(10));
            int before = HpDerivationLogic.ComputeMaxHp(12, 0, 0, 10);
            int after = HpDerivationLogic.ComputeMaxHp(12, 0, 0, 11);
            Assert.AreEqual(1, after - before);
        }

        [Test]
        public void BarbarianL1_HasHigherBaseThanHuman()
        {
            int human = HpDerivationLogic.ComputeMaxHp(12, 0, 0, 10);
            int barbarian = HpDerivationLogic.ComputeMaxHp(18, 0, 0, 12);
            Assert.Greater(barbarian, human);
        }

        [Test]
        public void LevelTable_IsPrimaryHpGrowth()
        {
            int l1 = HpDerivationLogic.ComputeMaxHp(12, 4, 0, 10);
            int l5 = HpDerivationLogic.ComputeMaxHp(12, 4, 16, 12); // 4 levels × 4 HP + Con growth
            Assert.AreEqual(26, l1);
            Assert.AreEqual(44, l5);
            Assert.Greater(l5 - l1, 12); // more than Con-only would give under C1 for +2 Con
        }

        [Test]
        public void CharacterStats_MaxHp_UsesDualTrack()
        {
            var go = new GameObject("hp_actor");
            try
            {
                CharacterStats stats = go.AddComponent<CharacterStats>();
                stats.race = Race.Human;
                stats.humanClass = HumanClass.None;
                stats.raceBaseHP = 12;
                stats.levelHpBonus = 0;
                stats.Constitution = new Stat(10);
                Assert.AreEqual(22, stats.MaxHP);
                Assert.AreEqual(50, stats.EncumbranceLimit);

                stats.humanClass = HumanClass.Knight;
                Assert.AreEqual(26, stats.MaxHP);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }

    [TestFixture]
    public class DamageApplicationLogicTests
    {
        [Test]
        public void SwordFullArmor_AppliesFullMitigation()
        {
            // AC 14 → Full mit 2; no resist
            int final = DamageApplicationLogic.ComputeFinalDamage(12, 0, 14, ArmorInteraction.Full);
            Assert.AreEqual(10, final);
        }

        [Test]
        public void FireballPartial_ResistThenPartialArmor()
        {
            // Fire resist 4, AC 14 → Partial mit 1 → 12-4-1 = 7
            int final = DamageApplicationLogic.ComputeFinalDamage(12, 4, 14, ArmorInteraction.Partial);
            Assert.AreEqual(7, final);
        }

        [Test]
        public void PoisonNone_IgnoresArmor()
        {
            int final = DamageApplicationLogic.ComputeFinalDamage(5, 0, 14, ArmorInteraction.None);
            Assert.AreEqual(5, final);
        }

        [Test]
        public void FireResist_BluntsMoreThanPartialArmorAlone()
        {
            int withResist = DamageApplicationLogic.ComputeFinalDamage(12, 4, 14, ArmorInteraction.Partial);
            int armorOnly = DamageApplicationLogic.ComputeFinalDamage(12, 0, 14, ArmorInteraction.Partial);
            Assert.Less(withResist, armorOnly);
            Assert.AreEqual(11, armorOnly);
        }

        [Test]
        public void MinimumOneDamage_WhenResistExceedsRaw()
        {
            int final = DamageApplicationLogic.ComputeFinalDamage(5, 99, 14, ArmorInteraction.Full);
            Assert.AreEqual(1, final);
        }
    }

    [TestFixture]
    public class AttackDamageLogicTests
    {
        [Test]
        public void MeleeStrengthBonus_IsFloorDiv4()
        {
            Assert.AreEqual(2, AttackDamageLogic.MeleeStrengthBonus(10));
            Assert.AreEqual(3, AttackDamageLogic.MeleeStrengthBonus(14));
            Assert.AreEqual(5, AttackDamageLogic.ApplyMeleeStrengthBonus(2, 14));
        }

        [Test]
        public void AttributeModifierMath_DnDStyle()
        {
            Assert.AreEqual(0, AttributeModifierMath.Modifier(10));
            Assert.AreEqual(3, AttributeModifierMath.Modifier(16));
            Assert.AreEqual(-1, AttributeModifierMath.Modifier(8));
        }
    }
}
