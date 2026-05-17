using System.Collections.Generic;
using JRogue.Ability.Passive;
using JRogue.Item.Essence;
using JRogue.Racial;
using JRogue.Stats;
using JRogue.Stats.Racial;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.Racial
{
    public class ElementalSpiritContractsRuntimeTests
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
        }

        [Test]
        public void Summon_AppliesCumulativeStats_DismissRemoves()
        {
            ElementalSpiritDefinition spirit = BuildSpiritWithStats();
            GameObject go = CreateElf(spirit, contractLevel: 2);
            var runtime = go.GetComponent<ElementalSpiritContractsRuntime>();
            CharacterStats stats = go.GetComponent<CharacterStats>();
            stats.currentSoulPower = 20;

            Assert.IsTrue(runtime.TrySummon(spirit.spiritId, out _));
            Assert.AreEqual(12, stats.Dexterity.GetValue());
            Assert.IsTrue(runtime.IsSpiritSummoned(spirit.spiritId));

            Assert.IsTrue(runtime.TryDismiss(spirit.spiritId));
            Assert.AreEqual(10, stats.Dexterity.GetValue());
            Assert.IsFalse(runtime.IsSpiritSummoned(spirit.spiritId));
        }

        [Test]
        public void UpkeepFailure_AutoDismisses()
        {
            ElementalSpiritDefinition spirit = BuildSpiritWithStats();
            spirit.upkeepSoulPowerPerTurn = 5;
            GameObject go = CreateElf(spirit, contractLevel: 1);
            var runtime = go.GetComponent<ElementalSpiritContractsRuntime>();
            CharacterStats stats = go.GetComponent<CharacterStats>();
            stats.currentSoulPower = 10;
            runtime.TrySummon(spirit.spiritId, out _);
            stats.currentSoulPower = 0;

            runtime.NotifyTurnStart();

            Assert.IsFalse(runtime.IsSpiritSummoned(spirit.spiritId));
        }

        [Test]
        public void FireImbueToggle_AddsWeaponBonus()
        {
            ElementalSpiritDefinition spirit = BuildSpiritWithFireToggle();
            GameObject go = CreateElf(spirit, contractLevel: 1);
            var runtime = go.GetComponent<ElementalSpiritContractsRuntime>();
            go.GetComponent<CharacterStats>().currentSoulPower = 20;
            runtime.TrySummon(spirit.spiritId, out _);

            var imbue = spirit.levels[0].activeEntries[0].ability as FireWeaponImbueAbility;
            Assert.IsNotNull(imbue);
            Assert.IsTrue(imbue.Execute(go));
            Assert.AreEqual(3, runtime.WeaponFireImbueBonus);
            Assert.IsTrue(imbue.Execute(go));
            Assert.AreEqual(0, runtime.WeaponFireImbueBonus);
        }

        [Test]
        public void CannotSummon_UncontractedSpirit()
        {
            ElementalSpiritDefinition spirit = BuildSpiritWithStats();
            GameObject go = CreateElf(spirit, contractLevel: 1);
            var runtime = go.GetComponent<ElementalSpiritContractsRuntime>();
            go.GetComponent<CharacterStats>().currentSoulPower = 20;

            Assert.IsFalse(runtime.TrySummon("unknown_spirit", out string reason));
            Assert.IsNotEmpty(reason);
        }

        GameObject CreateElf(ElementalSpiritDefinition spirit, int contractLevel)
        {
            var go = new GameObject("ElfTest");
            _destroy.Add(go);
            var stats = go.AddComponent<CharacterStats>();
            stats.race = Race.Elf;
            stats.racialSubsystem = RacialSubsystemKind.ElfElementalContracts;
            var runtime = go.AddComponent<ElementalSpiritContractsRuntime>();
            var field = typeof(ElementalSpiritContractsRuntime).GetField(
                "contractedSpirits",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(runtime, new List<ElementalSpiritContractPreset>
            {
                new ElementalSpiritContractPreset { spirit = spirit, contractLevel = contractLevel }
            });
            return go;
        }

        static ElementalSpiritDefinition BuildSpiritWithStats()
        {
            var spirit = ScriptableObject.CreateInstance<ElementalSpiritDefinition>();
            spirit.spiritId = "test_spirit";
            spirit.displayName = "Test";
            spirit.maxLevel = 2;
            spirit.summonSoulPowerCost = 1;
            spirit.upkeepSoulPowerPerTurn = 1;
            spirit.levels = new List<ElementalSpiritLevelData>
            {
                new ElementalSpiritLevelData
                {
                    statModifiers = new List<AttributeModifier>
                    {
                        new AttributeModifier { attribute = StatType.Dexterity, value = 1 }
                    },
                    passiveEffects = new List<PassiveEffect>(),
                    activeEntries = new List<ElementalSpiritActiveEntry>
                    {
                        new ElementalSpiritActiveEntry
                        {
                            ability = ScriptableObject.CreateInstance<FireWeaponImbueAbility>()
                        }
                    }
                },
                new ElementalSpiritLevelData
                {
                    statModifiers = new List<AttributeModifier>
                    {
                        new AttributeModifier { attribute = StatType.Dexterity, value = 1 }
                    },
                    passiveEffects = new List<PassiveEffect>(),
                    activeEntries = new List<ElementalSpiritActiveEntry>()
                }
            };
            var imbue = (FireWeaponImbueAbility)spirit.levels[0].activeEntries[0].ability;
            imbue.spiritId = spirit.spiritId;
            imbue.soulPowerCost = 0;
            return spirit;
        }

        static ElementalSpiritDefinition BuildSpiritWithFireToggle()
        {
            var spirit = BuildSpiritWithStats();
            var imbue = ScriptableObject.CreateInstance<FireWeaponImbueAbility>();
            imbue.spiritId = spirit.spiritId;
            imbue.soulPowerCost = 0;
            imbue.fireDamageBonus = 3;
            spirit.levels[0].activeEntries[0].ability = imbue;
            spirit.levels[0].activeEntries[0].kind = ElementalSpiritActiveKind.Toggle;
            spirit.levels[0].activeEntries[0].repeatableSameTurn = true;
            spirit.levels[0].activeEntries[0].consumesTurn = false;
            return spirit;
        }
    }
}
