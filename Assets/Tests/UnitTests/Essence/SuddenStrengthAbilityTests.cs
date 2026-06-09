using System.Collections.Generic;
using JRogue.Ability.SuddenStrength;
using JRogue.Actors;
using JRogue.Item.Essence;
using JRogue.Manager.Essence;
using JRogue.Manager.Party;
using JRogue.Stats;
using JRogue.Tests.UnitTests.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace JRogue.Tests.UnitTests.Essence
{
    [TestFixture]
    public sealed class SuddenStrengthAbilityTests
    {
        readonly List<GameObject> _created = new List<GameObject>();
        readonly List<Object> _assets = new List<Object>();

        [SetUp]
        public void SetUp() => InputTestSceneBuilder.ResetSingletonManagersForTests();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _created)
            {
                if (go != null)
                    Object.DestroyImmediate(go);
            }

            _created.Clear();

            foreach (Object asset in _assets)
            {
                if (asset != null)
                    Object.DestroyImmediate(asset);
            }

            _assets.Clear();

            InputTestSceneBuilder.ResetSingletonManagersForTests();
        }

        [Test]
        public void CanExecute_FalseWhenRuntimePresent()
        {
            SetupActorWithSuddenStrength(out BaseActor actor, out SuddenStrengthAbility ability, out _);
            actor.gameObject.AddComponent<SuddenStrengthBuffRuntime>().Apply(100, 10);

            LogAssert.Expect(LogType.Log, $"[Sudden Strength] Already active on {actor.name}.");

            Assert.IsFalse(ability.CanExecute(actor.gameObject));
        }

        [Test]
        public void IsReadyForUse_FalseWhenRuntimePresent_WithoutLogging()
        {
            SetupActorWithSuddenStrength(out BaseActor actor, out SuddenStrengthAbility ability, out _);
            actor.gameObject.AddComponent<SuddenStrengthBuffRuntime>().Apply(100, 10);

            Assert.IsFalse(ability.IsReadyForUse(actor.gameObject));
        }

        [Test]
        public void ExecuteCore_AddsModifierWithRuntimeSource()
        {
            SetupActorWithSuddenStrength(out BaseActor actor, out SuddenStrengthAbility ability, out _);
            int baseStr = actor.stats.Strength.GetValue();

            Assert.IsTrue(ability.Execute(actor.gameObject));
            Assert.AreEqual(baseStr + 100, actor.stats.Strength.GetValue());

            SuddenStrengthBuffRuntime runtime = actor.GetComponent<SuddenStrengthBuffRuntime>();
            Assert.IsNotNull(runtime);
            Assert.IsTrue(actor.stats.Strength.HasModifierFromSource(runtime));
            Assert.AreEqual(10, runtime.TurnsRemaining);
        }

        [Test]
        public void TenPlayerPhaseTicks_RemovesModifier()
        {
            SetupActorWithSuddenStrength(out BaseActor actor, out EssenceSlotManager essence, out _);
            int baseStr = actor.stats.Strength.GetValue();

            Assert.IsTrue(essence.TryExecuteAbility(0, 0));
            SuddenStrengthBuffRuntime runtime = actor.GetComponent<SuddenStrengthBuffRuntime>();
            Assert.IsNotNull(runtime);

            for (int i = 0; i < 9; i++)
                essence.NotifyTurnStart();

            Assert.IsNotNull(actor.GetComponent<SuddenStrengthBuffRuntime>());
            Assert.AreEqual(1, runtime.TurnsRemaining);
            Assert.AreEqual(baseStr + 100, actor.stats.Strength.GetValue());

            LogAssert.Expect(LogType.Log, $"[Sudden Strength] Expired on {actor.name}.");
            essence.NotifyTurnStart();

            Assert.IsNull(actor.GetComponent<SuddenStrengthBuffRuntime>());
            Assert.AreEqual(baseStr, actor.stats.Strength.GetValue());
        }

        [Test]
        public void TryExecuteAbility_DoesNotDeductSoulPowerWhenAlreadyActive()
        {
            SetupActorWithSuddenStrength(out BaseActor actor, out EssenceSlotManager essence, out _);
            Assert.IsTrue(essence.TryExecuteAbility(0, 0));

            int spAfterFirst = actor.stats.currentSoulPower;

            LogAssert.Expect(LogType.Log, $"[Sudden Strength] Already active on {actor.name}.");
            LogAssert.Expect(LogType.Log, "Sudden Strength conditions not met!");

            Assert.IsFalse(essence.TryExecuteAbility(0, 0));
            Assert.AreEqual(spAfterFirst, actor.stats.currentSoulPower);
        }

        [Test]
        public void DestroyRuntime_RemovesModifier()
        {
            SetupActorWithSuddenStrength(out BaseActor actor, out SuddenStrengthAbility ability, out _);
            int baseStr = actor.stats.Strength.GetValue();
            Assert.IsTrue(ability.Execute(actor.gameObject));

            SuddenStrengthBuffRuntime runtime = actor.GetComponent<SuddenStrengthBuffRuntime>();
            Assert.AreEqual(baseStr + 100, actor.stats.Strength.GetValue());

            Object.DestroyImmediate(runtime);

            Assert.IsNull(actor.GetComponent<SuddenStrengthBuffRuntime>());
            Assert.AreEqual(baseStr, actor.stats.Strength.GetValue());
        }

        [Test]
        public void PerActorIsolation_OnlyCasterGetsBuff()
        {
            InputTestSceneBuilder.SetupMapAndManagers(_created);
            PartyManager party = InputTestSceneBuilder.CreatePartyWithTestActors(2, _created);

            SuddenStrengthAbility ability = CreateAbilityAsset();
            EssenceData essenceData = CreateEssenceAsset(ability);

            BaseActor actorA = party.partyMembers[0];
            BaseActor actorB = party.partyMembers[1];
            EssenceSlotManager slotsA = actorA.GetComponent<EssenceSlotManager>();
            EssenceSlotManager slotsB = actorB.GetComponent<EssenceSlotManager>();
            slotsA.EquipEssence(essenceData, 0);
            slotsB.EquipEssence(essenceData, 0);
            actorA.stats.currentSoulPower = 5;
            actorB.stats.currentSoulPower = 5;

            int baseA = actorA.stats.Strength.GetValue();
            int baseB = actorB.stats.Strength.GetValue();

            Assert.IsTrue(slotsA.TryExecuteAbility(0, 0));
            Assert.AreEqual(baseA + 100, actorA.stats.Strength.GetValue());
            Assert.AreEqual(baseB, actorB.stats.Strength.GetValue());
            Assert.IsTrue(slotsB.TryExecuteAbility(0, 0));
            Assert.AreEqual(baseB + 100, actorB.stats.Strength.GetValue());
        }

        void SetupActorWithSuddenStrength(
            out BaseActor actor,
            out SuddenStrengthAbility ability,
            out EssenceSlotManager essence)
        {
            SetupActorWithSuddenStrength(out actor, out essence, out ability);
        }

        void SetupActorWithSuddenStrength(
            out BaseActor actor,
            out EssenceSlotManager essence,
            out SuddenStrengthAbility ability)
        {
            InputTestSceneBuilder.SetupMapAndManagers(_created);
            PartyManager party = InputTestSceneBuilder.CreatePartyWithTestActors(1, _created);
            actor = party.partyMembers[0];
            essence = actor.GetComponent<EssenceSlotManager>();

            ability = CreateAbilityAsset();
            essence.EquipEssence(CreateEssenceAsset(ability), 0);
            actor.stats.currentSoulPower = 10;
        }

        SuddenStrengthAbility CreateAbilityAsset()
        {
            var ability = ScriptableObject.CreateInstance<SuddenStrengthAbility>();
            ability.abilityName = "Sudden Strength";
            ability.soulPowerCost = 1;
            ability.requiresTarget = false;
            ability.strengthBonus = 100;
            ability.durationTurns = 10;
            _assets.Add(ability);
            return ability;
        }

        EssenceData CreateEssenceAsset(SuddenStrengthAbility ability)
        {
            var essenceData = ScriptableObject.CreateInstance<EssenceData>();
            essenceData.statModifiers = new List<AttributeModifier>();
            essenceData.resistanceModifiers = new List<DamageResistanceModifier>();
            essenceData.complexPassives = new List<PassiveEffect>();
            essenceData.activeAbilities = new List<JRogue.Ability.AbilityAction> { ability };
            _assets.Add(essenceData);
            return essenceData;
        }
    }
}
