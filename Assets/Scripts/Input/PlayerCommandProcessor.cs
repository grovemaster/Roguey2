using System.Collections.Generic;
using JRogue.Ability;
using JRogue.Actors;
using JRogue.Actors.Components;
using JRogue.Core.Actor;
using JRogue.Manager.Equipment;
using JRogue.Manager.Essence;
using JRogue.Manager.Grid;
using JRogue.Manager.Floor;
using JRogue.Manager.Map;
using JRogue.Manager.Party;
using JRogue.Manager.Turn;
using JRogue.Service.Formation;
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
        }

        private InputState currentState = InputState.Normal;
        private PendingTargetedAbility? pendingTargetedAbility;

        private PartyManager partyManager;
        private TurnManager turnManager;
        private GridManager gridManager;
        private MapManager mapManager;
        private TargetingReticleView reticleView;

        public InputState CurrentState => currentState;

        public void SetReticleView(TargetingReticleView view) => reticleView = view;

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
            if (!turnManager.CanActorTakeAction(activeMember.gameObject)) return false;

            Vector3Int targetTile = activeMember.GridPosition + direction;
            Vector3Int oldPosition = activeMember.GridPosition;

            IBattleTarget occupant = gridManager.GetActorAt(targetTile);

            BaseActor swappableAlly = null;
            bool isAllySwap = occupant is BaseActor actor && partyManager.partyMembers.Contains(actor);
            if (isAllySwap) swappableAlly = (BaseActor)occupant;

            bool isEnemyBump = occupant != null && !isAllySwap;

            if (partyManager.IsFormationActive)
            {
                if (isEnemyBump
                    || FormationRushService.IsValidMove(mapManager, gridManager, targetTile, new Dictionary<BaseActor, Vector3Int>(), allowAllies: true))
                {
                    if (TryConfirmGatedMove(activeMember, direction, targetTile, isEnemyBump, formationActive: true, oldPosition))
                        return true;

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

            if (isAllySwap
                && swappableAlly != null
                && FormationRushService.IsValidMove(mapManager, gridManager, targetTile, new Dictionary<BaseActor, Vector3Int>(), allowAllies: true))
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

            if (TryConfirmGatedMove(activeMember, direction, targetTile, isEnemyBump, formationActive: false, oldPosition))
                return true;

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
            bool ok = pending.Source switch
            {
                PlayerAbilitySource.Essence =>
                    actorEssence != null
                    && actorEssence.TryExecuteAbility(pending.SlotIndex, pending.AbilityIndex, target),
                PlayerAbilitySource.EquipmentItem =>
                    equipManager != null
                    && equipManager.TryExecuteItemAbility(pending.SlotIndex, pending.AbilityIndex, target),
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

        private bool ApplyCancelTarget()
        {
            if (currentState != InputState.Targeting) return false;
            ExitTargetingMode();
            Debug.Log("Targeted Ability Cancelled.");
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

            AbilityAction abilityToTry = fromEquipment
                ? equipManager?.GetItemAbility(slotIndex, abilityIndex)
                : actorEssence?.GetAbility(slotIndex, abilityIndex);

            if (abilityToTry == null) return false;

            if (abilityToTry.requiresTarget)
            {
                if (!fromEquipment && actorEssence != null && !actorEssence.CanAfford(slotIndex, abilityIndex))
                {
                    Debug.Log("Not enough Soul Power!");
                    return false;
                }

                PlayerAbilitySource source = fromEquipment
                    ? PlayerAbilitySource.EquipmentItem
                    : PlayerAbilitySource.Essence;

                EnterTargetingMode(actor, abilityToTry, source, slotIndex, abilityIndex);
                return true;
            }

            bool success = fromEquipment
                ? equipManager != null && equipManager.TryExecuteItemAbility(slotIndex, abilityIndex)
                : actorEssence != null && actorEssence.TryExecuteAbility(slotIndex, abilityIndex);

            if (success)
            {
                turnManager.OnPlayerActionComplete(actor.gameObject);
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
    }
}
