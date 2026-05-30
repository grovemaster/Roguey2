using System.Collections.Generic;
using System.Reflection;
using JRogue.Actors;
using JRogue.Actors.Components;
using JRogue.Hazards;
using JRogue.Manager.Combat;
using JRogue.Manager.Party;
using JRogue.Manager.Progression;
using JRogue.Manager.Turn;
using JRogue.Status;
using JRogue.Stats;
using JRogue.Stats.Racial;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.Progression
{
    [TestFixture]
    public sealed class RestSessionTests
    {
        readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            if (RestSessionService.Instance != null)
                RestSessionService.Instance.ResetForTests();

            foreach (GameObject go in _created)
            {
                if (go != null)
                    Object.DestroyImmediate(go);
            }

            _created.Clear();

            if (TurnManager.Instance != null)
                Object.DestroyImmediate(TurnManager.Instance.gameObject);

            if (PartyManager.Instance != null)
                Object.DestroyImmediate(PartyManager.Instance.gameObject);

            if (CombatThreatCoordinator.Instance != null)
                Object.DestroyImmediate(CombatThreatCoordinator.Instance.gameObject);

            if (HazardService.Instance != null)
                Object.DestroyImmediate(HazardService.Instance.gameObject);
        }

        [Test]
        public void ComputeHealBudget_FirstRest_IsTwentyPercentMaxHp()
        {
            PartyRestState state = CreatePartyRestState();
            CharacterStats stats = CreateStats(hp: 80, maxHp: 100);

            int budget = PartyRestState.ComputeHealBudgetForMember(
                stats,
                stats.gameObject.GetEntityId(),
                state);

            Assert.AreEqual(20, budget);
        }

        [Test]
        public void ComputeHealBudget_AfterDamage_IsTwentyPercentHpLostSinceLastRestStart()
        {
            PartyRestState state = CreatePartyRestState();
            CharacterStats stats = CreateStats(hp: 70, maxHp: 100);
            EntityId id = stats.gameObject.GetEntityId();
            state.SetHpSnapshotForTests(id, 90);

            int budget = PartyRestState.ComputeHealBudgetForMember(stats, id, state);

            Assert.AreEqual(4, budget);
        }

        [Test]
        public void CanStartRest_DeniedInCombat()
        {
            CreateTurnManager(GameState.PLAYER_TURN);
            CreateCombatCoordinator(inCombat: true);
            CreatePartyWithMember(soul: 5, maxSoul: 10);

            Assert.IsFalse(RestSessionService.CanStartRest(out string reason, out _));
            Assert.That(reason, Does.Contain("combat"));
        }

        [Test]
        public void CanStartRest_DeniedWithNegativeStatus_NotPoisonIdSpecific()
        {
            CreateTurnManager(GameState.PLAYER_TURN);
            CreateCombatCoordinator(inCombat: false);
            BaseActor actor = CreatePartyWithMember(soul: 5, maxSoul: 10);
            StatusEffectController statuses = actor.GetComponent<StatusEffectController>();

            var slowed = ScriptableObject.CreateInstance<StatusEffectDefinition>();
            slowed.statusId = StatusEffectId.Slowed;
            slowed.polarity = StatusPolarity.Negative;
            slowed.maxDurationTurns = 3;
            StatusEffectService.TryApply(statuses, slowed);

            Assert.IsFalse(RestSessionService.CanStartRest(out string reason, out _));
            Assert.That(reason, Does.Contain("negative status"));
        }

        [Test]
        public void CanStartRest_NotNecessaryWhenFullSoulAndNoHealBudget()
        {
            CreateTurnManager(GameState.PLAYER_TURN);
            CreateCombatCoordinator(inCombat: false);
            CreatePartyWithMember(soul: 10, maxSoul: 10, hp: 100, maxHp: 100);

            Assert.IsFalse(RestSessionService.CanStartRest(out _, out bool nothing));
            Assert.IsTrue(nothing);
        }

        [Test]
        public void TickRestHeal_RespectsSessionBudget()
        {
            PartyRestState state = CreatePartyRestState();
            CharacterStats stats = CreateStats(hp: 90, maxHp: 100);
            var members = new List<BaseActor> { stats.GetComponent<BaseActor>() };
            state.CommitSuccessfulRestStart(members);

            int healed = state.TickRestHeal(members[0]);
            Assert.AreEqual(1, healed);
            Assert.AreEqual(91, stats.currentHP);
            Assert.AreEqual(19, state.GetSessionHealRemaining(stats.gameObject.GetEntityId()));
        }

        PartyRestState CreatePartyRestState()
        {
            var go = new GameObject("PartyRestState");
            _created.Add(go);
            return go.AddComponent<PartyRestState>();
        }

        CharacterStats CreateStats(int hp, int maxHp = 100)
        {
            var go = new GameObject("Actor");
            _created.Add(go);
            var stats = go.AddComponent<CharacterStats>();
            stats.Constitution = new Stat(Mathf.Max(1, maxHp / 10));
            stats.currentHP = hp;
            stats.Intelligence = new Stat(10);
            stats.Wisdom = new Stat(10);
            stats.humanClass = HumanClass.None;
            stats.currentSoulPower = 5;
            go.AddComponent<HealthComponent>();
            go.AddComponent<StatusEffectController>();
            go.AddComponent<TestPartyActor>();
            return stats;
        }

        BaseActor CreatePartyWithMember(int soul, int maxSoul, int hp = 50, int maxHp = 100)
        {
            var partyGo = new GameObject("PartyManager");
            _created.Add(partyGo);
            PartyManager party = partyGo.AddComponent<PartyManager>();
            partyGo.AddComponent<PartyRestState>();
            partyGo.AddComponent<RestSessionService>();
            party.partyMembers = new List<BaseActor>();

            CharacterStats stats = CreateStats(hp, maxHp);
            stats.Intelligence = new Stat(1);
            stats.Wisdom = new Stat(1);
            stats.currentSoulPower = soul;
            stats.levelSoulPowerBonus = Mathf.Max(0, maxSoul - 10);

            BaseActor actor = stats.GetComponent<BaseActor>();
            party.partyMembers.Add(actor);
            return actor;
        }

        static TurnManager CreateTurnManager(GameState state)
        {
            var go = new GameObject("TurnManager");
            TurnManager tm = go.AddComponent<TurnManager>();
            tm.currentState = state;
            return tm;
        }

        static void CreateCombatCoordinator(bool inCombat)
        {
            var go = new GameObject("CombatThreat");
            go.AddComponent<CombatThreatCoordinator>();
            if (!inCombat)
                return;

            FieldInfo tensionField = typeof(CombatThreatCoordinator).GetField(
                "_tension",
                BindingFlags.Instance | BindingFlags.NonPublic);
            tensionField?.SetValue(CombatThreatCoordinator.Instance, CombatTensionState.InCombat);
        }

        sealed class TestPartyActor : BaseActor
        {
            protected override void Die() { }
        }
    }
}
