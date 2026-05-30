using System.Collections.Generic;
using JRogue.Ability;
using JRogue.Actors;
using JRogue.Item;
using JRogue.Manager.Inventory;
using JRogue.Actors.Components;
using JRogue.Core.Actor;
using JRogue.Manager.Equipment;
using JRogue.Manager.Essence;
using JRogue.Manager.Grid;
using JRogue.Manager.Floor;
using JRogue.Manager.Map;
using JRogue.Manager.Party;
using JRogue.Manager.Turn;
using JRogue.Racial;
using JRogue.Hazards;
using JRogue.Traps;
using JRogue.Interactables;
using JRogue.Combat;
using JRogue.Service.Formation;
using JRogue.UI.Gameplay;
using JRogue.Stats;
using JRogue.Stats.Racial;
using JRogue.UI.Targeting;
using UnityEngine;

namespace JRogue.Input
{
    /// <summary>
    /// Applies <see cref="PlayerCommand"/> to the game simulation. Single path for live input and replays.
    /// </summary>
    public sealed class PlayerCommandProcessor
    {
        private struct PendingTargetedAbility
        {
            public PlayerAbilitySource Source;
            public int SlotIndex;
            public int AbilityIndex;
            public AbilityAction InventoryAbility;
            public ItemInstance InventoryItemInstance;
            public BaseActor InventoryOwner;
            public int InventoryResumeSelectionIndex;
            public string InventoryLogTag;
            public ItemInstance BowRestoreOffHandInstance;
            public int BowInventoryResumeIndex;
        }

        private InputState currentState = InputState.Normal;
        private PendingTargetedAbility? pendingTargetedAbility;
        private System.Action<int> inventoryTargetedUseCancelCallback;

        private PartyManager partyManager;
        private TurnManager turnManager;
        private GridManager gridManager;
        private MapManager mapManager;
        private TargetingReticleView reticleView;

        public InputState CurrentState => currentState;

        public bool IsPendingInventoryTargetedUse =>
            currentState == InputState.Targeting
            && pendingTargetedAbility.HasValue
            && pendingTargetedAbility.Value.Source == PlayerAbilitySource.InventoryItem;

        public bool IsPendingBowAim =>
            currentState == InputState.Targeting
            && pendingTargetedAbility.HasValue
            && pendingTargetedAbility.Value.Source == PlayerAbilitySource.BowAim;

        public bool IsPendingBowOrInventoryTargeting =>
            IsPendingInventoryTargetedUse || IsPendingBowAim;

        public void SetReticleView(TargetingReticleView view) => reticleView = view;

        public void SetInventoryTargetedUseCancelCallback(System.Action<int> callback) =>
            inventoryTargetedUseCancelCallback = callback;

        /// <summary>Begin targeting for a carried item (inventory closed by UI before call).</summary>
        public bool TryBeginInventoryTargetedUse(
            BaseActor activeMember,
            AbilityAction ability,
            ItemInstance itemInstance,
            BaseActor itemOwner,
            int resumeSelectionIndex,
            string logTag)
        {
            if (ability == null || activeMember == null)
                return false;

            EnsureManagers();

            if (currentState == InputState.Targeting)
                return false;

            if (!turnManager.CanActorTakeAction(activeMember.gameObject))
                return false;

            currentState = InputState.Targeting;
            pendingTargetedAbility = new PendingTargetedAbility
            {
                Source = PlayerAbilitySource.InventoryItem,
                InventoryAbility = ability,
                InventoryItemInstance = itemInstance,
                InventoryOwner = itemOwner,
                InventoryResumeSelectionIndex = resumeSelectionIndex,
                InventoryLogTag = logTag ?? string.Empty,
            };

            InventoryTargetedUseLog.Log(logTag, "Use started; inventory closed; targeting active.");
            reticleView?.Show(activeMember.GridPosition);
            return true;
        }

        /// <summary>Begin bow targeting. Optional restore instance for invoke-arrow cancel.</summary>
        public bool TryBeginBowAim(
            BaseActor activeMember,
            ItemInstance restoreOffHandOnCancel = null,
            int inventoryResumeIndex = -1)
        {
            if (activeMember == null)
                return false;

            EnsureManagers();

            if (currentState == InputState.Targeting)
                return false;

            if (!turnManager.CanActorTakeAction(activeMember.gameObject))
                return false;

            if (!BowRangedCombatService.HasBowEquipped(activeMember))
            {
                Debug.Log("[Bow] Cannot aim: no bow equipped.");
                return false;
            }

            EquipmentManager equip = activeMember.GetComponent<EquipmentManager>();
            equip?.TryEnsureDefaultAmmoEquipped();

            if (!BowRangedCombatService.HasAnyArrowAvailable(activeMember))
            {
                Debug.Log("[Bow] Cannot shoot: no arrows.");
                return false;
            }

            currentState = InputState.Targeting;
            pendingTargetedAbility = new PendingTargetedAbility
            {
                Source = PlayerAbilitySource.BowAim,
                BowRestoreOffHandInstance = restoreOffHandOnCancel,
                BowInventoryResumeIndex = inventoryResumeIndex,
            };

            Debug.Log("[Bow] Aim started; targeting active.");
            reticleView?.Show(activeMember.GridPosition);
            return true;
        }

        /// <summary>
        /// Returns false when the command is ignored (wrong turn, invalid context, no-op move, etc.).
        /// Party swap matches legacy input and is accepted regardless of turn phase.
        /// </summary>
        public bool TryApply(PlayerCommand command)
        {
            if (command.Kind != PlayerCommandKind.SwapPartyMember && !IsPlayerTurnActive()) return false;

            switch (command.Kind)
            {
                case PlayerCommandKind.MoveGrid:
                    return ApplyMoveGrid(command.Direction);
                case PlayerCommandKind.Wait:
                    return ApplyWait(command.PartyWait);
                case PlayerCommandKind.ConfirmTarget:
                    return ApplyConfirmTarget();
                case PlayerCommandKind.CancelTarget:
                    return ApplyCancelTarget();
                case PlayerCommandKind.AbilitySlot:
                    return ApplyAbilitySlot(command.SlotIndex, command.AbilitySecondary, command.AbilityFromEquipment);
                case PlayerCommandKind.ToggleFormation:
                    return ApplyToggleFormation();
                case PlayerCommandKind.SwapPartyMember:
                    return ApplySwapPartyMember(command.PartyMemberIndex);
                case PlayerCommandKind.PickupFloorItems:
                    return ApplyPickupFloorItems();
                case PlayerCommandKind.AimBow:
                    return ApplyAimBow();
                default:
                    return false;
            }
        }

        public void ProcessFollowerRush()
        {
            EnsureManagers();
            FormationRushService.Rush(partyManager, turnManager, gridManager, mapManager);
        }

        private bool IsPlayerTurnActive() =>
            TurnManager.Instance != null && TurnManager.Instance.currentState == GameState.PLAYER_TURN;

        private void EnsureManagers()
        {
            if (partyManager == null) partyManager = PartyManager.Instance;
            if (turnManager == null) turnManager = TurnManager.Instance;
            if (gridManager == null) gridManager = GridManager.Instance;
            if (mapManager == null) mapManager = MapManager.Instance;
        }

        private bool ApplyMoveGrid(Vector3Int direction)
        {
            EnsureManagers();

            if (currentState == InputState.Targeting)
            {
                if (direction != Vector3Int.zero) reticleView?.Move(direction);
                return true;
            }

            if (direction == Vector3Int.zero) return false;

            BaseActor activeMember = partyManager.GetActiveMember();
            if (activeMember == null) return false;
            if (!turnManager.CanActorTakeAction(activeMember.gameObject))
            {
                Debug.Log($"{activeMember.name} has already moved! Switch characters or end turn.");
                return false;
            }

            Vector3Int targetTile = activeMember.GridPosition + direction;
            Vector3Int oldPosition = activeMember.GridPosition;

            IBattleTarget occupant = gridManager.GetActorAt(targetTile);

            BaseActor swappableAlly = null;
            bool isAllySwap = occupant is BaseActor actor && partyManager.partyMembers.Contains(actor);
            if (isAllySwap) swappableAlly = (BaseActor)occupant;

            bool isEnemyBump = occupant != null && !isAllySwap;

            bool isInteractableBump = InteractableTileService.Instance != null
                && InteractableTileService.Instance.ShouldAttemptPlayerBump(
                    activeMember.GridPosition,
                    targetTile);

            if (isAllySwap
                && swappableAlly != null
                && FormationRushService.IsValidMove(
                    mapManager,
                    gridManager,
                    targetTile,
                    new Dictionary<BaseActor, Vector3Int>(),
                    allowAllies: true,
                    follower: activeMember))
            {
                GridMover leaderMover = activeMember.GetComponent<GridMover>();
                GridMover allyMover = swappableAlly.GetComponent<GridMover>();
                if (GridMover.TrySwap(leaderMover, allyMover))
                {
                    Debug.Log($"[MANUAL-SWAP] {activeMember.name} swapped with {swappableAlly.name}");
                    turnManager.OnPlayerActionComplete(activeMember.gameObject);
                }

                return true;
            }

            bool formationActive = partyManager.IsFormationActive;
            if (TryConfirmGatedMove(
                    activeMember,
                    direction,
                    targetTile,
                    isEnemyBump,
                    formationActive,
                    oldPosition))
            {
                return true;
            }

            if (formationActive)
            {
                if (isEnemyBump
                    || isInteractableBump
                    || FormationRushService.IsValidMove(
                        mapManager,
                        gridManager,
                        targetTile,
                        new Dictionary<BaseActor, Vector3Int>(),
                        allowAllies: true,
                        follower: activeMember))
                {
                    if (activeMember.TryMove(direction))
                    {
                        if (activeMember.GridPosition != oldPosition)
                            partyManager.RecordNewLeaderPosition(activeMember.GridPosition);
                        else
                        {
                            Debug.Log($"[FORMATION-BUMP] Leader attacked at {targetTile}. Position stayed {oldPosition}.");
                            partyManager.SnapHistoryToCurrentPositions();
                        }

                        ProcessFollowerRush();
                    }
                }

                return true;
            }

            if (activeMember.TryMove(direction))
                turnManager.OnPlayerActionComplete(activeMember.gameObject);

            return true;
        }

        bool ApplyPickupFloorItems()
        {
            if (currentState == InputState.Targeting)
                return false;

            EnsureManagers();
            BaseActor activeMember = partyManager.GetActiveMember();
            if (activeMember == null)
                return false;

            return FloorPickupCoordinator.TryBeginManualPickup(activeMember);
        }

        bool TryConfirmGatedMove(
            BaseActor activeMember,
            Vector3Int direction,
            Vector3Int targetTile,
            bool isEnemyBump,
            bool formationActive,
            Vector3Int oldPosition)
        {
            if (TrapMoveGate.TryInterceptMove(
                    activeMember,
                    targetTile,
                    isEnemyBump,
                    () => CompleteConfirmedMove(activeMember, direction, formationActive, oldPosition)))
            {
                return true;
            }

            if (HazardMoveGate.TryInterceptMove(
                    activeMember,
                    targetTile,
                    isEnemyBump,
                    () => CompleteConfirmedMove(activeMember, direction, formationActive, oldPosition)))
            {
                return true;
            }

            return AutoPickupMoveGate.TryInterceptMove(
                activeMember,
                targetTile,
                isEnemyBump,
                () => CompleteConfirmedMove(activeMember, direction, formationActive, oldPosition));
        }

        void CompleteConfirmedMove(
            BaseActor activeMember,
            Vector3Int direction,
            bool formationActive,
            Vector3Int oldPosition)
        {
            if (!activeMember.TryMove(direction))
                return;

            Vector3Int dest = activeMember.GridPosition;
            // Silent auto-pickup already ran via GridMover.Moved → ManaStoneAutoPickupService.
            FloorPickupService.PickupConfirmGatedAt(dest, activeMember.gameObject);

            if (formationActive)
            {
                if (activeMember.GridPosition != oldPosition)
                    partyManager.RecordNewLeaderPosition(activeMember.GridPosition);
                else
                    partyManager.SnapHistoryToCurrentPositions();

                ProcessFollowerRush();
            }
            else
            {
                turnManager.OnPlayerActionComplete(activeMember.gameObject);
            }
        }

        private bool ApplyWait(bool partyWait)
        {
            EnsureManagers();

            BaseActor activeMember = partyManager.GetActiveMember();
            if (activeMember == null) return false;

            if (!turnManager.CanActorTakeAction(activeMember.gameObject))
            {
                Debug.Log($"{activeMember.name} has already moved! Switch characters or end turn.");
                return false;
            }

            if (partyWait)
            {
                Debug.Log("Party is skipping turns...");
                if (partyManager.IsFormationActive) ProcessFollowerRush();
                turnManager.ForceEndPlayerTurn();
                return true;
            }

            Debug.Log($"{activeMember.name} is skipping turn...");
            HazardService.Instance?.OnActorWaitOnCell(activeMember);

            if (partyManager.IsFormationActive)
            {
                ProcessFollowerRush();
                turnManager.OnPlayerActionComplete(activeMember.gameObject);
            }
            else
            {
                turnManager.OnPlayerActionComplete(activeMember.gameObject);
            }

            return true;
        }

        private bool ApplyConfirmTarget()
        {
            if (currentState != InputState.Targeting || !pendingTargetedAbility.HasValue) return false;

            EnsureManagers();

            BaseActor activeMember = partyManager.GetActiveMember();
            if (activeMember == null || reticleView == null) return false;

            PendingTargetedAbility pending = pendingTargetedAbility.Value;
            EssenceSlotManager actorEssence = activeMember.GetComponent<EssenceSlotManager>();
            EquipmentManager equipManager = activeMember.GetComponent<EquipmentManager>();

            Vector3Int target = reticleView.Position;
            HumanMageSpellsRuntime mageSpells = activeMember.GetComponent<HumanMageSpellsRuntime>();

            bool ok = pending.Source switch
            {
                PlayerAbilitySource.Essence =>
                    actorEssence != null
                    && actorEssence.TryExecuteAbility(pending.SlotIndex, pending.AbilityIndex, target),
                PlayerAbilitySource.EquipmentItem =>
                    equipManager != null
                    && equipManager.TryExecuteItemAbility(pending.SlotIndex, pending.AbilityIndex, target),
                PlayerAbilitySource.HumanMageSpell =>
                    mageSpells != null
                    && mageSpells.TryExecuteEquipped(pending.SlotIndex, activeMember.gameObject, target),
                PlayerAbilitySource.InventoryItem =>
                    TryExecuteInventoryItemTargetedUse(pending, activeMember, target),
                PlayerAbilitySource.BowAim =>
                    BowRangedCombatService.TryExecuteBowShot(activeMember, target, 1),
                _ => false,
            };

            if (!ok) return false;

            ExitTargetingMode();

            if (partyManager.IsFormationActive)
            {
                partyManager.RecordNewLeaderPosition(activeMember.GridPosition);
                ProcessFollowerRush();
                turnManager.ForceEndPlayerTurn();
            }
            else
            {
                turnManager.OnPlayerActionComplete(activeMember.gameObject);
            }

            Debug.Log($"Targeted ability executed. Leader now at: {activeMember.GridPosition}");
            return true;
        }

        bool ApplyAimBow()
        {
            EnsureManagers();
            BaseActor activeMember = partyManager.GetActiveMember();
            if (activeMember == null)
                return false;

            return TryBeginBowAim(activeMember);
        }

        static void RestoreBowOffHandAfterCancel(BaseActor actor, PendingTargetedAbility pending)
        {
            if (actor == null || pending.BowRestoreOffHandInstance == null)
                return;

            EquipmentManager equip = actor.GetComponent<EquipmentManager>();
            if (equip == null)
                return;

            ItemInstance restore = pending.BowRestoreOffHandInstance;
            ItemInstance current = equip.GetEquippedInstance(EquipmentSlot.OffHand);
            if (current != null && current.Id == restore.Id)
                return;

            if (current != null)
                equip.TryUnequipToBag(EquipmentSlot.OffHand);

            InventoryManager inv = actor.GetComponent<InventoryManager>();
            if (inv == null)
                return;

            foreach (ItemInstance c in inv.CarriedItems)
            {
                if (c != null && c.Id == restore.Id)
                {
                    equip.EquipItem(EquipmentSlot.OffHand, restore);
                    return;
                }
            }
        }

        private bool ApplyCancelTarget()
        {
            if (currentState != InputState.Targeting) return false;

            EnsureManagers();
            BaseActor activeMember = partyManager.GetActiveMember();

            if (pendingTargetedAbility.HasValue)
            {
                PendingTargetedAbility pending = pendingTargetedAbility.Value;

                if (pending.Source == PlayerAbilitySource.InventoryItem)
                {
                    if (inventoryTargetedUseCancelCallback != null)
                    {
                        inventoryTargetedUseCancelCallback.Invoke(pending.InventoryResumeSelectionIndex);
                        InventoryTargetedUseLog.Log(
                            pending.InventoryLogTag,
                            "Cancelled; item retained; inventory reopened; selection restored.");
                    }
                    else
                    {
                        InventoryTargetedUseLog.LogWarning(
                            pending.InventoryLogTag,
                            "Cancel with no pending scroll state.");
                    }
                }
                else if (pending.Source == PlayerAbilitySource.BowAim)
                {
                    RestoreBowOffHandAfterCancel(activeMember, pending);
                    if (pending.BowInventoryResumeIndex >= 0 && inventoryTargetedUseCancelCallback != null)
                        inventoryTargetedUseCancelCallback.Invoke(pending.BowInventoryResumeIndex);
                    Debug.Log("[Bow] Aim cancelled; no arrow consumed.");
                }
                else
                {
                    Debug.Log("Targeted Ability Cancelled.");
                }
            }

            ExitTargetingMode();
            return true;
        }

        static bool TryExecuteInventoryItemTargetedUse(
            PendingTargetedAbility pending,
            BaseActor activeMember,
            Vector3Int target)
        {
            AbilityAction ability = pending.InventoryAbility;
            if (ability == null || activeMember == null)
                return false;

            if (!ability.Execute(activeMember.gameObject, target))
            {
                InventoryTargetedUseLog.Log(pending.InventoryLogTag, $"Confirm rejected at {target}.");
                return false;
            }

            BaseActor itemOwner = pending.InventoryOwner;
            InventoryManager inventory = itemOwner != null ? itemOwner.GetComponent<InventoryManager>() : null;
            if (inventory != null
                && pending.InventoryItemInstance != null
                && !inventory.TryConsumeCarriedQuantity(pending.InventoryItemInstance, 1))
            {
                InventoryTargetedUseLog.LogWarning(
                    pending.InventoryLogTag,
                    $"Execute succeeded but TryConsumeCarriedQuantity failed for {pending.InventoryItemInstance.Id}.");
            }

            InventoryTargetedUseLog.Log(
                pending.InventoryLogTag,
                $"Confirm success at {target}; item consumed; turn ended.");
            return true;
        }

        private bool ApplyAbilitySlot(int slotIndex, bool isShift, bool isCtrl)
        {
            EnsureManagers();

            BaseActor activeMember = partyManager.GetActiveMember();
            if (activeMember == null) return false;

            if (!turnManager.CanActorTakeAction(activeMember.gameObject))
            {
                Debug.LogWarning($"[INPUT] {activeMember.name} has already acted and cannot use abilities.");
                return false;
            }

            int abilityIndex = isShift ? 1 : 0;
            return ProcessAbilityInput(activeMember, slotIndex, abilityIndex, isCtrl);
        }

        private bool ProcessAbilityInput(BaseActor actor, int slotIndex, int abilityIndex, bool fromEquipment)
        {
            EssenceSlotManager actorEssence = actor.GetComponent<EssenceSlotManager>();
            EquipmentManager equipManager = actor.GetComponent<EquipmentManager>();
            HumanMageSpellsRuntime mageSpells = actor.GetComponent<HumanMageSpellsRuntime>();
            CharacterStats stats = actor.stats;

            bool isMage = stats != null && stats.humanClass == HumanClass.Mage;

            AbilityAction abilityToTry;
            PlayerAbilitySource source;

            if (fromEquipment)
            {
                abilityToTry = equipManager?.GetItemAbility(slotIndex, abilityIndex);
                source = PlayerAbilitySource.EquipmentItem;
            }
            else if (isMage && mageSpells != null)
            {
                abilityToTry = mageSpells.GetEquippedAbility(abilityIndex);
                source = PlayerAbilitySource.HumanMageSpell;
                slotIndex = abilityIndex;
            }
            else
            {
                abilityToTry = actorEssence?.GetAbility(slotIndex, abilityIndex);
                source = PlayerAbilitySource.Essence;
            }

            if (abilityToTry == null) return false;

            if (abilityToTry.requiresTarget)
            {
                if (source == PlayerAbilitySource.HumanMageSpell)
                {
                    if (mageSpells == null || !mageSpells.CanAffordCast(abilityIndex))
                    {
                        Debug.Log("Not enough Magic Power!");
                        return false;
                    }
                }
                else if (!fromEquipment && actorEssence != null && !actorEssence.CanAfford(slotIndex, abilityIndex))
                {
                    Debug.Log("Not enough Soul Power!");
                    return false;
                }

                EnterTargetingMode(actor, abilityToTry, source, slotIndex, abilityIndex);
                return true;
            }

            bool success = source switch
            {
                PlayerAbilitySource.EquipmentItem =>
                    equipManager != null && equipManager.TryExecuteItemAbility(slotIndex, abilityIndex),
                PlayerAbilitySource.HumanMageSpell =>
                    mageSpells != null && mageSpells.TryExecuteEquipped(abilityIndex, actor.gameObject),
                _ => actorEssence != null && actorEssence.TryExecuteAbility(slotIndex, abilityIndex),
            };

            if (success)
            {
                if (partyManager.IsFormationActive)
                {
                    partyManager.RecordNewLeaderPosition(actor.GridPosition);
                    ProcessFollowerRush();
                }
                else
                {
                    turnManager.OnPlayerActionComplete(actor.gameObject);
                }
            }

            return true;
        }

        private bool ApplyToggleFormation()
        {
            EnsureManagers();

            BaseActor activeMember = partyManager.GetActiveMember();
            if (activeMember == null) return false;

            bool hasActed = !turnManager.CanActorTakeAction(activeMember.gameObject);

            if (!partyManager.IsFormationActive)
            {
                if (hasActed)
                {
                    Debug.LogWarning($"[FORMATION] Cannot enable: {activeMember.name} has already taken an action.");
                    return false;
                }

                partyManager.ToggleFormationActive();
                partyManager.SnapHistoryToCurrentPositions();
                Debug.Log($"[FORMATION] Enabled. {activeMember.name} is now the leader.");
            }
            else
            {
                partyManager.ToggleFormationActive();
                Debug.Log("[FORMATION] Disabled. Party members will move individually.");
            }

            return true;
        }

        private bool ApplySwapPartyMember(int zeroBasedIndex)
        {
            EnsureManagers();
            partyManager.SwapActiveMember(zeroBasedIndex);
            partyManager.SnapHistoryToCurrentPositions();

            BaseActor newActive = partyManager.GetActiveMember();
            if (newActive != null)
            {
                Debug.Log($"[SWAP] Now controlling {newActive.name}. Camera following and History Snapped.");
            }

            return true;
        }

        private void EnterTargetingMode(
            BaseActor actor,
            AbilityAction ability,
            PlayerAbilitySource source,
            int slotIndex,
            int abilityIndex)
        {
            currentState = InputState.Targeting;
            pendingTargetedAbility = new PendingTargetedAbility
            {
                Source = source,
                SlotIndex = slotIndex,
                AbilityIndex = abilityIndex,
            };

            Debug.Log($"Entered Targeting Mode for {ability.abilityName}. Move reticle, then confirm.");
            reticleView?.Show(actor.GridPosition);
        }

        private void ExitTargetingMode()
        {
            currentState = InputState.Normal;
            pendingTargetedAbility = null;
            reticleView?.Hide();
        }

        /// <summary>Exits targeting without inventory restore (e.g. active member died).</summary>
        public void ForceExitTargeting()
        {
            if (currentState != InputState.Targeting)
                return;

            ExitTargetingMode();
        }
    }
}
