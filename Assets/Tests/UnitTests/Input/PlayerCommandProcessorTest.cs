using System.Collections.Generic;
using JRogue.Ability.SuddenStrength;
using JRogue.Ability.Telekinesis;
using JRogue.Actors;
using JRogue.Input;
using JRogue.Item.Essence;
using JRogue.Manager.Essence;
using JRogue.Manager.Floor;
using JRogue.Manager.Party;
using JRogue.Manager.Turn;
using JRogue.Tests.Mocks;
using JRogue.UI.Targeting;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace JRogue.Tests.UnitTests.Input
{
    [TestFixture]
    public sealed class PlayerCommandProcessorTest
    {
        private readonly List<GameObject> _createdObjects = new List<GameObject>();
        private readonly List<Object> _scriptableCleanup = new List<Object>();

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
            foreach (GameObject o in _createdObjects)
            {
                if (o != null)
                    Object.DestroyImmediate(o);
            }

            _createdObjects.Clear();

            foreach (Object asset in _scriptableCleanup)
            {
                if (asset != null)
                    Object.DestroyImmediate(asset);
            }

            _scriptableCleanup.Clear();

            InputTestSceneBuilder.ResetSingletonManagersForTests();
        }

        [Test]
        public void TryApply_MoveGrid_ZeroDirection_ReturnsFalse()
        {
            SetupTwoMemberParty(out _, out PlayerCommandProcessor processor);

            Assert.IsFalse(processor.TryApply(PlayerCommand.MoveGrid(Vector3Int.zero)));
        }

        [Test]
        public void TryApply_MoveGrid_WhenEnemyTurn_ReturnsFalse()
        {
            SetupTwoMemberParty(out PartyManager _, out PlayerCommandProcessor processor);
            TurnManager.Instance.currentState = GameState.ENEMY_TURN;

            Assert.IsFalse(processor.TryApply(PlayerCommand.MoveGrid(Vector3Int.right)));
        }

        [Test]
        public void TryApply_SwapPartyMember_WhenEnemyTurn_SucceedsAndReordersLeader()
        {
            SetupTwoMemberParty(out PartyManager party, out PlayerCommandProcessor processor);
            TurnManager.Instance.currentState = GameState.ENEMY_TURN;

            BaseActor member0 = party.partyMembers[0];
            BaseActor member1 = party.partyMembers[1];

            Assert.IsTrue(processor.TryApply(PlayerCommand.SwapPartyMember(1)));

            Assert.AreSame(member1, party.partyMembers[0]);
            Assert.AreSame(member0, party.partyMembers[1]);
        }

        [Test]
        public void TryApply_MoveGrid_ManualMode_CompletesLeaderTurn()
        {
            SetupTwoMemberParty(out PartyManager party, out PlayerCommandProcessor processor);
            InputTestSceneBuilder.SetPrivateField(party, "isFormationActive", false);

            BaseActor leader = party.partyMembers[0];
            InputTestSceneBuilder.RegisterCurrentPartyOnGrid(party.partyMembers);

            Assert.IsTrue(TurnManager.Instance.CanActorTakeAction(leader.gameObject));
            Assert.IsTrue(processor.TryApply(PlayerCommand.MoveGrid(Vector3Int.right)));

            Assert.IsFalse(
                TurnManager.Instance.CanActorTakeAction(leader.gameObject),
                "Manual move consumes the leader's turn via OnPlayerActionComplete.");
            Assert.AreEqual(new Vector3Int(1, 0, 0), leader.GridPosition);
        }

        [Test]
        public void TryApply_AbilitySlot_TargetableThenCancel_ExitsTargeting()
        {
            SetupSingleMemberPartyWithTargetAbility(out PartyManager party, out PlayerCommandProcessor processor);
            InputTestSceneBuilder.RegisterCurrentPartyOnGrid(party.partyMembers);
            InputTestSceneBuilder.SetPrivateField(party, "isFormationActive", false);

            Assert.IsTrue(processor.TryApply(PlayerCommand.AbilitySlot(0, false, false)));
            Assert.AreEqual(InputState.Targeting, processor.CurrentState);

            Assert.IsTrue(processor.TryApply(PlayerCommand.CancelTarget()));
            Assert.AreEqual(InputState.Normal, processor.CurrentState);
        }

        [Test]
        public void TryApply_AbilitySlot_TargetableThenConfirm_CompletesTurn()
        {
            // Two members: leader's action must not finish the whole squad phase (IsPartyDone stays false),
            // so TurnManager does not run EnemyTurnSequence and clear charactersWhoActed before we assert.
            // A solo party would immediately end the player phase; the empty enemy sequence then starts a
            // new player turn and the leader could act again — failing CanActorTakeAction == false.
            SetupPartyWithTargetAbilityOnLeader(partySize: 2, out PartyManager party, out PlayerCommandProcessor processor);
            InputTestSceneBuilder.RegisterCurrentPartyOnGrid(party.partyMembers);
            InputTestSceneBuilder.SetPrivateField(party, "isFormationActive", false);

            BaseActor leader = party.partyMembers[0];

            Assert.IsTrue(processor.TryApply(PlayerCommand.AbilitySlot(0, false, false)));
            Assert.IsTrue(processor.TryApply(PlayerCommand.ConfirmTarget()));

            Assert.AreEqual(InputState.Normal, processor.CurrentState);
            Assert.IsFalse(TurnManager.Instance.CanActorTakeAction(leader.gameObject));
            Assert.IsTrue(
                TurnManager.Instance.CanActorTakeAction(party.partyMembers[1].gameObject),
                "Other party member should still be able to act this squad turn.");
        }

        [Test]
        public void ConfirmTarget_NotInTargeting_ReturnsFalse()
        {
            SetupTwoMemberParty(out PartyManager _, out PlayerCommandProcessor processor);

            Assert.IsFalse(processor.TryApply(PlayerCommand.ConfirmTarget()));
        }

        [Test]
        public void Telekinesis_InvalidConfirm_KeepsTargetingAndSoulPower()
        {
            SetupPartyWithTelekinesisOnLeader(out PartyManager party, out PlayerCommandProcessor processor);
            InputTestSceneBuilder.RegisterCurrentPartyOnGrid(party.partyMembers);
            InputTestSceneBuilder.SetPrivateField(party, "isFormationActive", false);

            BaseActor leader = party.partyMembers[0];
            Vector3Int emptyTile = leader.GridPosition + Vector3Int.right;
            int spBefore = leader.stats.currentSoulPower;

            LogAssert.Expect(
                LogType.Log,
                $"[Telekinesis] Invalid target at tile ({emptyTile.x}, {emptyTile.y}, {emptyTile.z}).");

            Assert.IsTrue(processor.TryApply(PlayerCommand.AbilitySlot(0, false, false)));
            Assert.AreEqual(InputState.Targeting, processor.CurrentState);

            Assert.IsTrue(processor.TryApply(PlayerCommand.MoveGrid(Vector3Int.right)));
            Assert.IsFalse(processor.TryApply(PlayerCommand.ConfirmTarget()));

            Assert.AreEqual(InputState.Targeting, processor.CurrentState);
            Assert.AreEqual(spBefore, leader.stats.currentSoulPower);
            Assert.IsTrue(TurnManager.Instance.CanActorTakeAction(leader.gameObject));

            Assert.IsTrue(processor.TryApply(PlayerCommand.CancelTarget()));
            Assert.AreEqual(InputState.Normal, processor.CurrentState);
        }

        [Test]
        public void UntargetedAbility_InFormation_EndsPlayerTurnViaRush()
        {
            SetupPartyWithSuddenStrengthOnLeader(out PartyManager party, out PlayerCommandProcessor processor);
            InputTestSceneBuilder.RegisterCurrentPartyOnGrid(party.partyMembers);
            Assert.IsTrue(party.IsFormationActive);

            BaseActor leader = party.partyMembers[0];
            Assert.IsTrue(processor.TryApply(PlayerCommand.AbilitySlot(0, false, false)));

            Assert.IsFalse(TurnManager.Instance.CanActorTakeAction(leader.gameObject));
            Assert.IsFalse(processor.TryApply(PlayerCommand.MoveGrid(Vector3Int.down)));
            Assert.AreEqual(GameState.ENEMY_TURN, TurnManager.Instance.currentState);
        }

        void SetupPartyWithSuddenStrengthOnLeader(out PartyManager party, out PlayerCommandProcessor processor)
        {
            InputTestSceneBuilder.SetupMapAndManagers(_createdObjects);
            party = InputTestSceneBuilder.CreatePartyWithTestActors(2, _createdObjects);
            BaseActor leader = party.partyMembers[0];

            var ability = ScriptableObject.CreateInstance<SuddenStrengthAbility>();
            ability.abilityName = "Sudden Strength";
            ability.soulPowerCost = 1;
            ability.requiresTarget = false;
            ability.strengthBonus = 100;
            ability.durationTurns = 10;

            var essence = ScriptableObject.CreateInstance<EssenceData>();
            essence.statModifiers = new List<AttributeModifier>();
            essence.resistanceModifiers = new List<DamageResistanceModifier>();
            essence.complexPassives = new List<PassiveEffect>();
            essence.activeAbilities = new List<JRogue.Ability.AbilityAction> { ability };

            leader.GetComponent<EssenceSlotManager>().EquipEssence(essence, 0);
            leader.stats.currentSoulPower = 10;

            _scriptableCleanup.Add(ability);
            _scriptableCleanup.Add(essence);

            processor = NewProcessorWithReticle();
        }

        void SetupPartyWithTelekinesisOnLeader(out PartyManager party, out PlayerCommandProcessor processor)
        {
            InputTestSceneBuilder.SetupMapAndManagers(_createdObjects);
            if (FloorItemPileService.Instance == null)
            {
                var pileGo = new GameObject("FloorItemPileService");
                _createdObjects.Add(pileGo);
                pileGo.AddComponent<FloorItemPileService>();
            }

            party = InputTestSceneBuilder.CreatePartyWithTestActors(1, _createdObjects);
            BaseActor leader = party.partyMembers[0];

            var ability = ScriptableObject.CreateInstance<TelekinesisAbility>();
            ability.requiresTarget = true;
            ability.soulPowerCost = 1;
            ability.range = 7;

            var essence = ScriptableObject.CreateInstance<EssenceData>();
            essence.statModifiers = new List<AttributeModifier>();
            essence.resistanceModifiers = new List<DamageResistanceModifier>();
            essence.complexPassives = new List<PassiveEffect>();
            essence.activeAbilities = new List<JRogue.Ability.AbilityAction> { ability };

            leader.GetComponent<EssenceSlotManager>().EquipEssence(essence, 0);
            leader.stats.currentSoulPower = 10;

            _scriptableCleanup.Add(ability);
            _scriptableCleanup.Add(essence);

            processor = NewProcessorWithReticle();
        }

        [Test]
        public void ProcessFollowerRush_WithBreadcrumbs_MovesFollowersTowardHistoricSlots()
        {
            InputTestSceneBuilder.SetupMapAndManagers(_createdObjects);
            PartyManager party = InputTestSceneBuilder.CreatePartyWithTestActors(3, _createdObjects);
            PlayerCommandProcessor processor = NewProcessorWithReticle();

            BaseActor leader = party.partyMembers[0];

            leader.SetGridPosition(new Vector3Int(0, 0, 0));
            party.partyMembers[1].SetGridPosition(new Vector3Int(0, -2, 0));
            party.partyMembers[2].SetGridPosition(new Vector3Int(0, -4, 0));
            party.positionHistory = new List<Vector3Int>
            {
                new Vector3Int(0, 0, 0),
                new Vector3Int(0, -1, 0),
                new Vector3Int(0, -2, 0)
            };
            InputTestSceneBuilder.RegisterCurrentPartyOnGrid(party.partyMembers);

            party.RecordNewLeaderPosition(leader.GridPosition);
            processor.ProcessFollowerRush();

            Assert.AreEqual(new Vector3Int(0, -1, 0), party.partyMembers[1].GridPosition);
            Assert.AreEqual(new Vector3Int(0, -2, 0), party.partyMembers[2].GridPosition);
            Assert.AreEqual(GameState.PLAYER_TURN, TurnManager.Instance.currentState);

            foreach (BaseActor member in party.partyMembers)
            {
                Assert.IsTrue(
                    TurnManager.Instance.CanActorTakeAction(member.gameObject),
                    $"{member.name} should regain actions after FormationRush completes the squad sweep.");
            }
        }

        private PlayerCommandProcessor NewProcessorWithReticle()
        {
            GameObject reticleGo = new GameObject("Reticle_Test");
            _createdObjects.Add(reticleGo);
            TargetingReticleView view = reticleGo.AddComponent<TargetingReticleView>();

            var processor = new PlayerCommandProcessor();
            processor.SetReticleView(view);
            return processor;
        }

        private void SetupTwoMemberParty(out PartyManager party, out PlayerCommandProcessor processor)
        {
            InputTestSceneBuilder.SetupMapAndManagers(_createdObjects);
            party = InputTestSceneBuilder.CreatePartyWithTestActors(2, _createdObjects);
            processor = NewProcessorWithReticle();
        }

        private void SetupSingleMemberPartyWithTargetAbility(out PartyManager party, out PlayerCommandProcessor processor) =>
            SetupPartyWithTargetAbilityOnLeader(1, out party, out processor);

        private void SetupPartyWithTargetAbilityOnLeader(int partySize, out PartyManager party, out PlayerCommandProcessor processor)
        {
            InputTestSceneBuilder.SetupMapAndManagers(_createdObjects);
            party = InputTestSceneBuilder.CreatePartyWithTestActors(partySize, _createdObjects);

            BaseActor leader = party.partyMembers[0];

            var ability = ScriptableObject.CreateInstance<DummyTargetAbility>();
            ability.requiresTarget = true;
            ability.soulPowerCost = 0;

            var essence = ScriptableObject.CreateInstance<EssenceData>();
            essence.statModifiers = new List<AttributeModifier>();
            essence.resistanceModifiers = new List<DamageResistanceModifier>();
            essence.complexPassives = new List<PassiveEffect>();
            essence.activeAbilities = new List<JRogue.Ability.AbilityAction> { ability };

            EssenceSlotManager mgr = leader.GetComponent<EssenceSlotManager>();
            mgr.EquipEssence(essence, 0);
            leader.stats.currentSoulPower = 999;

            _scriptableCleanup.Add(ability);
            _scriptableCleanup.Add(essence);

            processor = NewProcessorWithReticle();
        }
    }
}
