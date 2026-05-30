using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Actors.Components;
using JRogue.Manager.Essence;
using JRogue.Manager.Party;
using JRogue.Manager.Turn;
using JRogue.Status;
using JRogue.Stats;
using JRogue.UI.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.Party
{
    [TestFixture]
    public sealed class MainCharacterGameOverTests
    {
        readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            PartyMemberDeathService.ResetForTests();
            GameOverService.ResetForTests();

            if (GameOverModalUI.IsVisible)
            {
                var modal = Object.FindAnyObjectByType<GameOverModalUI>();
                if (modal != null)
                    Object.DestroyImmediate(modal.gameObject);
            }

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
        }

        [Test]
        public void TryDesignateMainCharacter_SucceedsOnce_SecondAttemptFails()
        {
            PartyManager party = CreatePartyManager();
            BaseActor hero = CreatePartyActor("Hero");
            BaseActor recruit = CreatePartyActor("Recruit");
            party.partyMembers.Add(hero);
            party.partyMembers.Add(recruit);

            Assert.IsTrue(party.TryDesignateMainCharacter(hero));
            Assert.IsTrue(party.HasMainCharacter);
            Assert.AreEqual(hero, party.MainCharacter);

            Assert.IsFalse(party.TryDesignateMainCharacter(recruit));
            Assert.AreEqual(hero, party.MainCharacter);
        }

        [Test]
        public void SwapActiveMember_DoesNotChangeMainCharacter()
        {
            PartyManager party = CreatePartyManager();
            BaseActor main = CreatePartyActor("Main");
            BaseActor recruit = CreatePartyActor("Recruit");
            party.partyMembers.Add(recruit);
            party.partyMembers.Add(main);
            party.TryDesignateMainCharacter(main);

            party.SwapActiveMember(0);

            Assert.AreEqual(recruit, party.GetActiveMember());
            Assert.IsTrue(party.IsMainCharacter(main));
            Assert.IsFalse(party.IsMainCharacter(recruit));
        }

        [Test]
        public void HandleDeath_MainCharacter_EntersGameOver_AndBlocksGameplay()
        {
            CreateTurnManager();
            PartyManager party = CreatePartyManager();
            BaseActor main = CreatePartyActor("Main");
            BaseActor recruit = CreatePartyActor("Recruit");
            party.partyMembers.Add(main);
            party.partyMembers.Add(recruit);
            party.TryDesignateMainCharacter(main);
            main.stats.currentHP = 0;

            PartyMemberDeathService.HandleDeath(main);

            Assert.AreEqual(GameState.GAME_OVER, TurnManager.Instance.currentState);
            Assert.IsTrue(GameOverService.IsGameOver);
            Assert.IsTrue(GameOverModalUI.BlocksGameplay);
            Assert.IsFalse(TurnManager.Instance.CanActorTakeAction(recruit.gameObject));
            Assert.IsTrue(main != null);
        }

        [Test]
        public void HandleDeath_Recruit_DoesNotEnterGameOver()
        {
            CreateTurnManager();
            TurnManager.Instance.currentState = GameState.PLAYER_TURN;

            PartyManager party = CreatePartyManager();
            BaseActor main = CreatePartyActor("Main");
            BaseActor recruit = CreatePartyActor("Recruit");
            party.partyMembers.Add(main);
            party.partyMembers.Add(recruit);
            party.TryDesignateMainCharacter(main);
            recruit.stats.currentHP = 0;

            PartyMemberDeathService.HandleDeath(recruit);

            Assert.AreEqual(GameState.PLAYER_TURN, TurnManager.Instance.currentState);
            Assert.IsFalse(GameOverService.IsGameOver);
            Assert.IsFalse(GameOverModalUI.BlocksGameplay);
        }

        static TurnManager CreateTurnManager()
        {
            var go = new GameObject("TurnManager_Test");
            TurnManager tm = go.AddComponent<TurnManager>();
            tm.currentState = GameState.PLAYER_TURN;
            return tm;
        }

        BaseActor CreatePartyActor(string name)
        {
            GameObject go = new GameObject(name);
            _created.Add(go);
            go.AddComponent<CharacterStats>();
            go.AddComponent<HealthComponent>();
            go.AddComponent<EssenceSlotManager>();
            go.AddComponent<GridMover>();
            go.AddComponent<StatusEffectController>();
            return go.AddComponent<TestPartyActor>();
        }

        static PartyManager CreatePartyManager()
        {
            var go = new GameObject("PartyManager");
            PartyManager pm = go.AddComponent<PartyManager>();
            pm.partyMembers = new List<BaseActor>();
            return pm;
        }

        sealed class TestPartyActor : BaseActor
        {
            protected override void Die() { }
        }
    }
}
