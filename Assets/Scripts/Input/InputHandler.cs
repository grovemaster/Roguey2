using System.Collections.Generic;
using JRogue.Ability;
using JRogue.Actors;
using JRogue.Core.Actor;
using JRogue.Manager.Equipment;
using JRogue.Manager.Essence;
using JRogue.Manager.Grid;
using JRogue.Manager.Map;
using JRogue.Manager.Party;
using JRogue.Manager.Turn;
using JRogue.Service.Formation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace JRogue.Input
{
    public enum InputState { Normal, Targeting }

    public class InputHandler : MonoBehaviour
    {
        private GameControls controls;

        private InputState currentState = InputState.Normal;
        private Vector3Int reticlePosition;
        private AbilityAction pendingAbility;

        [Header("Targeting Visuals")]
        [SerializeField] private GameObject reticlePrefab;
        private GameObject activeReticle;

        // Cached singleton refs. Populated lazily via EnsureManagers() because
        // singleton .Instance may not be set yet during Awake order. Once
        // non-null, no further lookups happen.
        private PartyManager partyManager;
        private TurnManager turnManager;
        private GridManager gridManager;
        private MapManager mapManager;

        private void Awake()
        {
            controls = new GameControls();

            controls.Player.PrimaryAbilities.performed += ctx => OnAbilityPerformed(ctx, false, false);
            controls.Player.ShiftAbilities.performed += ctx => OnAbilityPerformed(ctx, true, false);
            controls.Player.CtrlAbilities.performed += ctx => OnAbilityPerformed(ctx, false, true);
            controls.Player.Confirm.performed += OnConfirm;
            controls.Player.Cancel.performed += OnCancel;

            controls.Player.SelectPartyMember.performed += SwapTo;
            controls.Player.ToggleFormation.performed += OnToggleFormation;
        }

        private void OnEnable() => controls.Player.Enable();
        private void OnDisable() => controls.Player.Disable();

        private void OnDestroy()
        {
            // Generated InputActionAsset wrappers implement IDisposable; release
            // bindings explicitly to avoid leaks across domain reloads.
            controls?.Dispose();
        }

        private void EnsureManagers()
        {
            if (partyManager == null) partyManager = PartyManager.Instance;
            if (turnManager == null) turnManager = TurnManager.Instance;
            if (gridManager == null) gridManager = GridManager.Instance;
            if (mapManager == null) mapManager = MapManager.Instance;
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            if (IsContextInvalid(context)) return;
            EnsureManagers();

            Vector2 input = context.ReadValue<Vector2>();
            Vector3Int direction = new Vector3Int(Mathf.RoundToInt(input.x), Mathf.RoundToInt(input.y), 0);

            if (currentState == InputState.Targeting)
            {
                MoveReticle(direction);
                return;
            }

            if (direction == Vector3Int.zero) return;

            BaseActor activeMember = partyManager.GetActiveMember();
            if (activeMember == null) return;
            if (!turnManager.CanActorTakeAction(activeMember.gameObject)) return;

            Vector3Int targetTile = activeMember.GridPosition + direction;
            Vector3Int oldPosition = activeMember.GridPosition;

            IBattleTarget occupant = gridManager.GetActorAt(targetTile);

            BaseActor swappableAlly = null;
            bool isAllySwap = occupant is BaseActor actor && partyManager.partyMembers.Contains(actor);
            if (isAllySwap) swappableAlly = (BaseActor)occupant;

            bool isEnemyBump = occupant != null && !isAllySwap;

            if (partyManager.IsFormationActive)
            {
                // Allow the action if it's a valid move OR an enemy bump.
                if (isEnemyBump
                    || FormationRushService.IsValidMove(mapManager, gridManager, targetTile, new Dictionary<BaseActor, Vector3Int>(), allowAllies: true))
                {
                    if (activeMember.TryMove(direction))
                    {
                        // Only record breadcrumbs when the position actually changed
                        // (prevents clustering when the leader bumped without moving).
                        if (activeMember.GridPosition != oldPosition)
                        {
                            partyManager.RecordNewLeaderPosition(activeMember.GridPosition);
                        }
                        else
                        {
                            Debug.Log($"[FORMATION-BUMP] Leader attacked at {targetTile}. Position stayed {oldPosition}.");
                            // After an attack-without-move, followers still need a valid 'current' target.
                            partyManager.SnapHistoryToCurrentPositions();
                        }

                        // Trigger Rush so followers catch up after the leader's action.
                        ProcessFollowerRush();
                    }
                }
                return;
            }

            // Manual mode: handle atomic swap with an ally, or a normal move.
            if (isAllySwap
                && swappableAlly != null
                && FormationRushService.IsValidMove(mapManager, gridManager, targetTile, new Dictionary<BaseActor, Vector3Int>(), allowAllies: true))
            {
                // 1. Lift both completely from the grid first
                gridManager.UnregisterActor(activeMember.GridPosition);
                gridManager.UnregisterActor(swappableAlly.GridPosition);

                // 2. Teleport them to their new grid coordinates
                activeMember.SetGridPosition(targetTile);
                swappableAlly.SetGridPosition(oldPosition);

                // 3. Re-register and sync visuals
                gridManager.RegisterActor(activeMember.GridPosition, activeMember);
                gridManager.RegisterActor(swappableAlly.GridPosition, swappableAlly);

                activeMember.SyncPosition();
                swappableAlly.SyncPosition();

                Debug.Log($"[MANUAL-SWAP] {activeMember.name} swapped with {swappableAlly.name}");
                turnManager.OnPlayerActionComplete(activeMember.gameObject);
            }
            else if (activeMember.TryMove(direction))
            {
                turnManager.OnPlayerActionComplete(activeMember.gameObject);
            }
        }

        public void OnWait(InputAction.CallbackContext context)
        {
            if (IsContextInvalid(context)) return;
            EnsureManagers();

            BaseActor activeMember = partyManager.GetActiveMember();
            if (activeMember == null) return;

            if (!turnManager.CanActorTakeAction(activeMember.gameObject))
            {
                Debug.Log($"{activeMember.name} has already moved! Switch characters or end turn.");
                return;
            }

            // Shift-modified wait = "Party Wait", which ends the player phase.
            bool isPartyWait = Keyboard.current.shiftKey.isPressed;
            if (isPartyWait)
            {
                Debug.Log("Party is skipping turns...");
                if (partyManager.IsFormationActive) ProcessFollowerRush();
                turnManager.ForceEndPlayerTurn();
                return;
            }

            Debug.Log($"{activeMember.name} is skipping turn...");
            if (partyManager.IsFormationActive)
            {
                // In Formation mode, even a leader's wait still rushes followers,
                // and the leader is the "clock" so the squad turn ends.
                ProcessFollowerRush();
                turnManager.OnPlayerActionComplete(activeMember.gameObject);
            }
            else
            {
                // Manual mode: only this character is marked done.
                turnManager.OnPlayerActionComplete(activeMember.gameObject);
            }
        }

        public void OnConfirm(InputAction.CallbackContext context)
        {
            if (!context.performed || currentState != InputState.Targeting) return;
            EnsureManagers();

            BaseActor activeMember = partyManager.GetActiveMember();
            if (activeMember == null || pendingAbility == null) return;

            if (pendingAbility.Execute(activeMember.gameObject, reticlePosition))
            {
                ExitTargetingMode();

                if (partyManager.IsFormationActive)
                {
                    // Sync history to the leader's new position so followers
                    // rush toward the post-ability tile (e.g., teleport target).
                    partyManager.RecordNewLeaderPosition(activeMember.GridPosition);
                    ProcessFollowerRush();
                    turnManager.ForceEndPlayerTurn();
                }
                else
                {
                    turnManager.OnPlayerActionComplete(activeMember.gameObject);
                }

                Debug.Log($"Targeted ability executed. Leader now at: {activeMember.GridPosition}");
            }
        }

        public void OnCancel(InputAction.CallbackContext context)
        {
            if (!context.performed || currentState != InputState.Targeting) return;
            ExitTargetingMode();
            Debug.Log("Targeted Ability Cancelled.");
        }

        public void OnToggleFormation(InputAction.CallbackContext context)
        {
            if (IsContextInvalid(context)) return;
            EnsureManagers();

            BaseActor activeMember = partyManager.GetActiveMember();
            if (activeMember == null) return;

            bool hasActed = !turnManager.CanActorTakeAction(activeMember.gameObject);

            if (!partyManager.IsFormationActive)
            {
                if (hasActed)
                {
                    Debug.LogWarning($"[FORMATION] Cannot enable: {activeMember.name} has already taken an action.");
                    return;
                }

                partyManager.ToggleFormationActive();
                // Mid-turn activation: snap history to current positions so the
                // breadcrumb trail starts where everyone is standing right now.
                partyManager.SnapHistoryToCurrentPositions();
                Debug.Log($"[FORMATION] Enabled. {activeMember.name} is now the leader.");
            }
            else
            {
                partyManager.ToggleFormationActive();
                Debug.Log("[FORMATION] Disabled. Party members will move individually.");
            }
        }

        private void OnAbilityPerformed(InputAction.CallbackContext context, bool isShift, bool isCtrl)
        {
            if (IsContextInvalid(context)) return;
            EnsureManagers();

            BaseActor activeMember = partyManager.GetActiveMember();
            if (activeMember == null) return;

            // Don't even open targeting if the actor has already acted this turn.
            if (!turnManager.CanActorTakeAction(activeMember.gameObject))
            {
                Debug.LogWarning($"[INPUT] {activeMember.name} has already acted and cannot use abilities.");
                return;
            }

            string keyName = context.control.name;
            if (!int.TryParse(keyName, out int numberPressed)) return;

            int slotIndex = numberPressed - 1;
            ProcessAbilityInput(activeMember, slotIndex, isShift, isCtrl);
        }

        private void ProcessAbilityInput(BaseActor actor, int slotIndex, bool isShift, bool isCtrl)
        {
            EssenceSlotManager actorEssence = actor.GetComponent<EssenceSlotManager>();
            EquipmentManager equipManager = actor.GetComponent<EquipmentManager>();

            // Modifiers select which ability to look up:
            //   Ctrl  => item ability (slot from EquipmentManager)
            //   Shift => second ability of essence in this slot
            //   else  => primary ability of essence in this slot
            int abilityIndex = isShift ? 1 : 0;
            AbilityAction abilityToTry = isCtrl
                ? equipManager?.GetItemAbility(slotIndex, abilityIndex)
                : actorEssence?.GetAbility(slotIndex, abilityIndex);

            if (abilityToTry == null) return;

            if (abilityToTry.requiresTarget)
            {
                EnterTargetingMode(actor, abilityToTry);
            }
            else if (abilityToTry.CanExecute(actor.gameObject))
            {
                if (abilityToTry.Execute(actor.gameObject))
                {
                    turnManager.OnPlayerActionComplete(actor.gameObject);
                }
            }
        }

        private void EnterTargetingMode(BaseActor actor, AbilityAction ability)
        {
            currentState = InputState.Targeting;
            pendingAbility = ability;
            reticlePosition = actor.GridPosition;

            Debug.Log($"Entered Targeting Mode for {ability.abilityName}. Move reticle, then confirm.");

            if (activeReticle == null) activeReticle = Instantiate(reticlePrefab);
            activeReticle.SetActive(true);
            UpdateReticleVisuals();
        }

        private void MoveReticle(Vector3Int direction)
        {
            reticlePosition += direction;
            UpdateReticleVisuals();
            Debug.Log($"Targeting Reticle moved to: {reticlePosition}");
        }

        private void UpdateReticleVisuals()
        {
            if (activeReticle == null) return;
            // Add 0.5f to align with tile centers (matches GridMover.SyncPosition).
            activeReticle.transform.position = new Vector3(reticlePosition.x + 0.5f, reticlePosition.y + 0.5f, 0);
        }

        private void ExitTargetingMode()
        {
            currentState = InputState.Normal;
            pendingAbility = null;
            if (activeReticle != null) activeReticle.SetActive(false);
        }

        private void SwapTo(InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            EnsureManagers();

            // Function keys arrive as "F1".."F5"; strip the prefix to get the index.
            string keyName = context.control.name;
            string suffix = keyName.Replace("F", "").Replace("f", "");
            if (!int.TryParse(suffix, out int numberPressed))
            {
                Debug.LogWarning($"Unrecognized key for party swap: {keyName}");
                return;
            }

            int index = numberPressed - 1;

            partyManager.SwapActiveMember(index);

            // Snap history to the new arrangement so the party doesn't try to rush
            // toward the previous leader's trail.
            partyManager.SnapHistoryToCurrentPositions();

            BaseActor newActive = partyManager.GetActiveMember();
            if (newActive != null)
            {
                Debug.Log($"[SWAP] Now controlling {newActive.name}. Camera following and History Snapped.");
            }
        }

        // Thin wrapper kept so existing tests can drive the rush via reflection
        // on this private name. The actual algorithm lives in FormationRushService.
        private void ProcessFollowerRush()
        {
            EnsureManagers();
            FormationRushService.Rush(partyManager, turnManager, gridManager, mapManager);
        }

        private bool IsContextInvalid(InputAction.CallbackContext context) =>
            !context.performed
            || TurnManager.Instance == null
            || TurnManager.Instance.currentState != GameState.PLAYER_TURN;
    }
}
