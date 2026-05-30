using System.Collections.Generic;
using System.Reflection;
using JRogue.Ability;
using JRogue.Ability.Heal;
using JRogue.Actors;
using JRogue.Actors.Components;
using JRogue.Input;
using JRogue.Item;
using JRogue.Manager.Combat;
using JRogue.Manager.Inventory;
using JRogue.Manager.Grid;
using JRogue.Manager.Map;
using JRogue.Manager.Party;
using JRogue.Manager.Progression;
using JRogue.Manager.Turn;
using JRogue.Racial;
using JRogue.Service.Formation;
using JRogue.Status;
using JRogue.Stats;
using JRogue.Stats.Racial;
using JRogue.Tests.UnitTests.Input;
using JRogue.UI.Inventory;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.Racial
{
    [TestFixture]
    public sealed class WarriorWillpowerHealingPotionTests
    {
        readonly List<GameObject> _created = new List<GameObject>();
        readonly List<Object> _assets = new List<Object>();

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
        public void DefaultBarbarianLoadout_GrantsWarriorWillpower()
        {
            var loadout = ScriptableObject.CreateInstance<RacialLoadoutDefinition>();
            _assets.Add(loadout);
            loadout.requiredRace = Race.Barbarian;
            loadout.grantedRacialTraits = RacialTraitFlags.WarriorWillpower;

            GameObject go = CreateActor(Race.Barbarian);
            loadout.Apply(go);

            Assert.IsTrue(RacialTraitQueries.HasTrait(go, RacialTraitFlags.WarriorWillpower));
        }

        [Test]
        public void HumanLoadout_DoesNotGrantWarriorWillpower()
        {
            GameObject go = CreateActor(Race.Human);
            Assert.IsFalse(RacialTraitQueries.HasTrait(go, RacialTraitFlags.WarriorWillpower));
        }

        [Test]
        public void NewCharacter_HasPainToleranceTen()
        {
            GameObject go = CreateActor(Race.Human);
            var stats = go.GetComponent<CharacterStats>();
            Assert.AreEqual(10, stats.painTolerance.GetValue());
        }

        [Test]
        public void ComputeStunTurns_ToleranceTen_IsTen()
        {
            GameObject go = CreateActor(Race.Human);
            var stats = go.GetComponent<CharacterStats>();
            Assert.AreEqual(10, HealingPotionRules.ComputeStunTurns(stats));
        }

        [Test]
        public void HealingPotion_HealsFiftyAndSpendsTurn()
        {
            SetupPlayerTurn(out PartyManager party, out TurnManager turn);
            BaseActor actor = party.GetActiveMember();
            actor.stats.currentHP = 40;
            actor.stats.Constitution = new Stat(10);

            InventoryViewModel.Row row = CreateHealingPotionRow(actor, qty: 2);
            InventoryUseResult result = InventoryItemUse.TryUseCarriedItem(row, inCombat: false);

            Assert.AreEqual(InventoryUseOutcome.ConsumedImmediately, result.Outcome);
            Assert.AreEqual(90, actor.stats.currentHP);
            Assert.AreEqual(1, row.Instance.Quantity);
            Assert.IsFalse(turn.CanActorTakeAction(actor.gameObject));
        }

        [Test]
        public void HealingPotion_InCombat_NonExempt_Blocked()
        {
            SetupPlayerTurn(out _, out _);
            BaseActor actor = PartyManager.Instance.GetActiveMember();
            SetCombat(inCombat: true);

            InventoryViewModel.Row row = CreateHealingPotionRow(actor);
            Assert.IsFalse(InventoryUsability.AppearsUsableNow(row, inCombat: true));

            InventoryUseResult result = InventoryItemUse.TryUseCarriedItem(row, inCombat: true);
            Assert.AreEqual(InventoryUseOutcome.Failed, result.Outcome);
            Assert.AreEqual(HealingPotionRules.CombatBanMessage, result.FailureReason);
        }

        [Test]
        public void HealingPotion_OutOfCombat_AppliesStunTenTurns()
        {
            SetupPlayerTurn(out _, out _);
            BaseActor actor = PartyManager.Instance.GetActiveMember();
            actor.stats.currentHP = 40;

            InventoryViewModel.Row row = CreateHealingPotionRow(actor);
            Assert.IsTrue(InventoryItemUse.TryUseCarriedItem(row, inCombat: false).Outcome
                          == InventoryUseOutcome.ConsumedImmediately);
            Assert.AreEqual(10, actor.GetComponent<StatusEffectController>().GetTurnsRemaining(StatusEffectId.Stunned));
        }

        [Test]
        public void HealingPotion_Exempt_CombatUse_NoStun()
        {
            SetupPlayerTurn(out _, out _);
            BaseActor actor = PartyManager.Instance.GetActiveMember();
            actor.stats.race = Race.Barbarian;
            actor.stats.racialTraits = RacialTraitFlags.WarriorWillpower;
            actor.stats.painTolerance.AddModifier(90, "test");
            actor.stats.currentHP = 50;
            SetCombat(inCombat: true);

            InventoryViewModel.Row row = CreateHealingPotionRow(actor);
            Assert.IsTrue(HealingPotionRules.IsExemptFromPainStun(actor.gameObject));
            Assert.IsTrue(InventoryUsability.AppearsUsableNow(row, inCombat: true));

            InventoryUseResult result = InventoryItemUse.TryUseCarriedItem(row, inCombat: true);
            Assert.AreEqual(InventoryUseOutcome.ConsumedImmediately, result.Outcome);
            Assert.AreEqual(100, actor.stats.currentHP);
            Assert.IsFalse(actor.GetComponent<StatusEffectController>().HasStatus(StatusEffectId.Stunned));
        }

        [Test]
        public void StunnedActor_CannotMove_WithoutSpendingTurn()
        {
            InputTestSceneBuilder.SetupMapAndManagers(_created);
            PartyManager party = InputTestSceneBuilder.CreatePartyWithTestActors(1, _created);
            InputTestSceneBuilder.SetPrivateField(party, "isFormationActive", false);
            InputTestSceneBuilder.RegisterCurrentPartyOnGrid(party.partyMembers);
            TurnManager.Instance.currentState = GameState.PLAYER_TURN;

            BaseActor leader = party.partyMembers[0];
            Vector3Int start = leader.GridPosition;
            ApplyStun(leader);

            var processor = new PlayerCommandProcessor();
            Assert.IsTrue(processor.TryApply(PlayerCommand.MoveGrid(Vector3Int.right)));
            Assert.AreEqual(start, leader.GridPosition);
            Assert.IsFalse(TurnManager.Instance.CanActorTakeAction(leader.gameObject));
        }

        [Test]
        public void StunnedFollower_FormationRush_DoesNotMove()
        {
            InputTestSceneBuilder.SetupMapAndManagers(_created);
            PartyManager party = InputTestSceneBuilder.CreatePartyWithTestActors(2, _created);
            InputTestSceneBuilder.RegisterCurrentPartyOnGrid(party.partyMembers);
            TurnManager.Instance.currentState = GameState.PLAYER_TURN;

            BaseActor follower = party.partyMembers[1];
            Vector3Int start = follower.GridPosition;
            ApplyStun(follower);

            FormationRushService.Rush(
                party,
                TurnManager.Instance,
                GridManager.Instance,
                MapManager.Instance);

            Assert.AreEqual(start, follower.GridPosition);
        }

        [Test]
        public void Stunned_BlocksRest()
        {
            CreateTurnManager(GameState.PLAYER_TURN);
            CreateCombatCoordinator(inCombat: false);
            CreatePartyWithMember();

            BaseActor actor = PartyManager.Instance.partyMembers[0];
            ApplyStun(actor);

            Assert.IsFalse(RestSessionService.CanStartRest(out string reason, out _));
            Assert.That(reason, Does.Contain("negative status"));
        }

        [Test]
        public void Stunned_HasNegativePolarity()
        {
            Assert.AreEqual(StatusPolarity.Negative, StatusEffectPolarityRules.GetDefaultPolarity(StatusEffectId.Stunned));
        }

        GameObject CreateActor(Race race)
        {
            GameObject go = new GameObject("Actor");
            _created.Add(go);
            var stats = go.AddComponent<CharacterStats>();
            stats.race = race;
            stats.Constitution = new Stat(10);
            stats.currentHP = stats.MaxHP;
            go.AddComponent<HealthComponent>();
            go.AddComponent<StatusEffectController>();
            return go;
        }

        void SetupPlayerTurn(out PartyManager party, out TurnManager turn)
        {
            InputTestSceneBuilder.SetupMapAndManagers(_created);
            party = InputTestSceneBuilder.CreatePartyWithTestActors(1, _created);
            InputTestSceneBuilder.RegisterCurrentPartyOnGrid(party.partyMembers);
            turn = TurnManager.Instance;
            turn.currentState = GameState.PLAYER_TURN;
            CreateCombatCoordinator(inCombat: false);
        }

        InventoryViewModel.Row CreateHealingPotionRow(BaseActor owner, int qty = 1)
        {
            var potionData = ScriptableObject.CreateInstance<ItemData>();
            _assets.Add(potionData);
            potionData.category = ItemCategory.Potion;
            potionData.itemName = "Healing Potion";

            var ability = ScriptableObject.CreateInstance<HealingPotionAbility>();
            _assets.Add(ability);
            var stunned = ScriptableObject.CreateInstance<StatusEffectDefinition>();
            _assets.Add(stunned);
            stunned.statusId = StatusEffectId.Stunned;
            stunned.polarity = StatusPolarity.Negative;
            stunned.displayName = "Stunned";
            SetPrivateField(ability, "stunnedDefinition", stunned);
            potionData.activeAbilities = new List<AbilityAction> { ability };

            var instance = new ItemInstance(potionData) { Quantity = qty };
            InventoryManager inventory = owner.GetComponent<InventoryManager>();
            if (inventory == null)
                inventory = owner.gameObject.AddComponent<InventoryManager>();
            inventory.AddItem(instance);

            return new InventoryViewModel.Row(
                'h',
                instance,
                owner,
                owner.DisplayName,
                isEquipped: false,
                equippedSlot: null,
                carriedListIndex: 0,
                stackedWeight: potionData.weight);
        }

        void ApplyStun(BaseActor actor)
        {
            var stunned = ScriptableObject.CreateInstance<StatusEffectDefinition>();
            _assets.Add(stunned);
            stunned.statusId = StatusEffectId.Stunned;
            stunned.polarity = StatusPolarity.Negative;
            StatusEffectService.TryApplyWithDuration(actor.GetComponent<StatusEffectController>(), stunned, 5);
        }

        void CreateTurnManager(GameState state)
        {
            GameObject go = new GameObject("TurnManager");
            _created.Add(go);
            go.AddComponent<TurnManager>();
            TurnManager.Instance.currentState = state;
        }

        void CreateCombatCoordinator(bool inCombat)
        {
            GameObject go = new GameObject("CombatThreatCoordinator");
            _created.Add(go);
            go.AddComponent<CombatThreatCoordinator>();
            if (!inCombat)
                return;

            FieldInfo tensionField = typeof(CombatThreatCoordinator).GetField(
                "tensionState",
                BindingFlags.Instance | BindingFlags.NonPublic);
            tensionField?.SetValue(CombatThreatCoordinator.Instance, CombatTensionState.InCombat);
        }

        void SetCombat(bool inCombat) => CreateCombatCoordinator(inCombat);

        void CreatePartyWithMember()
        {
            GameObject partyGo = new GameObject("PartyManager");
            _created.Add(partyGo);
            var party = partyGo.AddComponent<PartyManager>();
            GameObject memberGo = CreateActor(Race.Human);
            memberGo.AddComponent<BaseActor>();
            memberGo.AddComponent<InventoryManager>();
            party.partyMembers = new List<BaseActor> { memberGo.GetComponent<BaseActor>() };
        }

        static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field?.SetValue(target, value);
        }
    }
}
