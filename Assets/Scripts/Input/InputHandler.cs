using JRogue.Actors;
using JRogue.Ability;
using JRogue.Item;
using JRogue.Manager.Progression;
using JRogue.Manager.Turn;
using JRogue.UI.Gameplay;
using JRogue.UI.Inventory;
using JRogue.UI.Targeting;
using UnityEngine;
using UnityEngine.InputSystem;

namespace JRogue.Input
{
    public class InputHandler : MonoBehaviour
    {
        private GameControls controls;
        private InputAction toggleInventoryAction;
        private InputAction pickupFloorItemsAction;
        private InputAction aimBowAction;
        private InputAction restAction;
        private InputAction openDoorAction;
        private InputAction closeDoorAction;
        private InputAction interactAction;
        private InputAction moveAction;
        private InputAction waitAction;
        private InputAction confirmAction;
        private InputAction cancelAction;
        private readonly PlayerCommandProcessor commandProcessor = new PlayerCommandProcessor();

        public PlayerCommandProcessor CommandProcessor => commandProcessor;

        [Header("Targeting Visuals")]
        [SerializeField] private TargetingReticleView reticleView;

        private void Awake()
        {
            controls = new GameControls();

            if (reticleView == null)
                reticleView = GetComponent<TargetingReticleView>();

            if (reticleView == null)
            {
                Debug.LogError(
                    $"{nameof(InputHandler)} on '{gameObject.name}' needs a sibling {nameof(TargetingReticleView)} "
                    + $"(assign the serialized field or Add Component). Targeted abilities cannot show a reticle otherwise.");
            }

            commandProcessor.SetReticleView(reticleView);

            controls.Player.PrimaryAbilities.performed += ctx => OnAbilityPerformed(ctx, false, false);
            controls.Player.ShiftAbilities.performed += ctx => OnAbilityPerformed(ctx, true, false);
            controls.Player.CtrlAbilities.performed += ctx => OnAbilityPerformed(ctx, false, true);
            controls.Player.Confirm.performed += OnConfirm;
            controls.Player.Cancel.performed += OnCancel;

            controls.Player.SelectPartyMember.performed += SwapTo;
            controls.Player.ToggleFormation.performed += OnToggleFormation;

            // Same InputActionAsset instance as PlayerInput (standalone InputActions can miss pairing / routing).
            var playerInput = GetComponent<PlayerInput>();
            toggleInventoryAction = playerInput != null
                ? playerInput.actions.FindAction("ToggleInventory", throwIfNotFound: false)
                : null;

            if (toggleInventoryAction == null)
            {
                Debug.LogError(
                    $"{nameof(InputHandler)}: No <b>ToggleInventory</b> action found on this {nameof(PlayerInput)}. "
                    + "Reimport Assets/Controls/GameControls.inputactions (binding <Keyboard>/i).");
            }
            else
            {
                toggleInventoryAction.performed += OnToggleInventoryPerformed;
            }

            pickupFloorItemsAction = playerInput != null
                ? playerInput.actions.FindAction("PickupFloorItems", throwIfNotFound: false)
                : null;

            if (pickupFloorItemsAction == null)
            {
                Debug.LogWarning(
                    $"{nameof(InputHandler)}: No <b>PickupFloorItems</b> action on {nameof(PlayerInput)}. "
                    + "Add it to GameControls (bindings: comma, g).");
            }
            else
            {
                pickupFloorItemsAction.performed += OnPickupFloorItemsPerformed;
            }

            aimBowAction = playerInput != null
                ? playerInput.actions.FindAction("AimBow", throwIfNotFound: false)
                : null;

            if (aimBowAction == null)
            {
                Debug.LogWarning(
                    $"{nameof(InputHandler)}: No <b>AimBow</b> action on {nameof(PlayerInput)}. "
                    + "Add it to GameControls (binding <Keyboard>/a).");
            }
            else
            {
                aimBowAction.performed += OnAimBowPerformed;
            }

            restAction = playerInput != null
                ? playerInput.actions.FindAction("Rest", throwIfNotFound: false)
                : null;

            if (restAction == null)
            {
                Debug.LogWarning(
                    $"{nameof(InputHandler)}: No <b>Rest</b> action on {nameof(PlayerInput)}. "
                    + "Add it to GameControls (binding <Keyboard>/r).");
            }
            else
            {
                restAction.performed += OnRestPerformed;
            }

            openDoorAction = playerInput != null
                ? playerInput.actions.FindAction("OpenDoor", throwIfNotFound: false)
                : null;
            if (openDoorAction == null)
            {
                Debug.LogWarning(
                    $"{nameof(InputHandler)}: No <b>OpenDoor</b> action on {nameof(PlayerInput)}. "
                    + "Add it to GameControls (binding <Keyboard>/o).");
            }
            else
            {
                openDoorAction.performed += OnOpenDoorPerformed;
            }

            closeDoorAction = playerInput != null
                ? playerInput.actions.FindAction("CloseDoor", throwIfNotFound: false)
                : null;
            if (closeDoorAction == null)
            {
                Debug.LogWarning(
                    $"{nameof(InputHandler)}: No <b>CloseDoor</b> action on {nameof(PlayerInput)}. "
                    + "Add it to GameControls (binding <Keyboard>/c).");
            }
            else
            {
                closeDoorAction.performed += OnCloseDoorPerformed;
            }

            interactAction = playerInput != null
                ? playerInput.actions.FindAction("Interact", throwIfNotFound: false)
                : null;
            if (interactAction == null)
            {
                Debug.LogWarning(
                    $"{nameof(InputHandler)}: No <b>Interact</b> action on {nameof(PlayerInput)}. "
                    + "Add it to GameControls (binding <Keyboard>/e).");
            }
            else
            {
                interactAction.performed += OnInteractPerformed;
            }

            WireCoreGameplayActions(playerInput);

            AdjacentInteractPickerModalUI.EnsureInstance();
            AltarOfferingModalUI.EnsureInstance();
            AltarUsedModalUI.EnsureInstance();

            FloorPickupHudButton.EnsureInstance();
        }

        private void OnEnable() => controls.Player.Enable();
        private void OnDisable() => controls.Player.Disable();

        private void OnDestroy()
        {
            if (toggleInventoryAction != null)
            {
                toggleInventoryAction.performed -= OnToggleInventoryPerformed;
                toggleInventoryAction = null;
            }

            if (pickupFloorItemsAction != null)
            {
                pickupFloorItemsAction.performed -= OnPickupFloorItemsPerformed;
                pickupFloorItemsAction = null;
            }

            if (aimBowAction != null)
            {
                aimBowAction.performed -= OnAimBowPerformed;
                aimBowAction = null;
            }

            if (restAction != null)
            {
                restAction.performed -= OnRestPerformed;
                restAction = null;
            }

            if (openDoorAction != null)
            {
                openDoorAction.performed -= OnOpenDoorPerformed;
                openDoorAction = null;
            }

            if (closeDoorAction != null)
            {
                closeDoorAction.performed -= OnCloseDoorPerformed;
                closeDoorAction = null;
            }

            if (interactAction != null)
            {
                interactAction.performed -= OnInteractPerformed;
                interactAction = null;
            }

            if (moveAction != null)
            {
                moveAction.performed -= OnMove;
                moveAction = null;
            }

            if (waitAction != null)
            {
                waitAction.performed -= OnWait;
                waitAction = null;
            }

            if (confirmAction != null)
            {
                confirmAction.performed -= OnConfirm;
                confirmAction = null;
            }

            if (cancelAction != null)
            {
                cancelAction.performed -= OnCancel;
                cancelAction = null;
            }

            controls?.Dispose();
        }

        void WireCoreGameplayActions(PlayerInput playerInput)
        {
            if (playerInput?.actions == null)
                return;

            moveAction = playerInput.actions.FindAction("Move", throwIfNotFound: false);
            if (moveAction != null)
                moveAction.performed += OnMove;

            waitAction = playerInput.actions.FindAction("Wait", throwIfNotFound: false);
            if (waitAction != null)
                waitAction.performed += OnWait;

            confirmAction = playerInput.actions.FindAction("Confirm", throwIfNotFound: false);
            if (confirmAction != null)
                confirmAction.performed += OnConfirm;

            cancelAction = playerInput.actions.FindAction("Cancel", throwIfNotFound: false);
            if (cancelAction != null)
                cancelAction.performed += OnCancel;
        }

        void OnInteractPerformed(InputAction.CallbackContext context)
        {
            if (!context.performed || BlocksFloorGameplay())
                return;

            commandProcessor.TryApply(PlayerCommand.Interact());
        }

        void OnPickupFloorItemsPerformed(InputAction.CallbackContext context)
        {
            if (!context.performed)
                return;

            if (BlocksFloorGameplay())
                return;

            commandProcessor.TryApply(PlayerCommand.PickupFloorItems());
        }

        void OnToggleInventoryPerformed(InputAction.CallbackContext context)
        {
            if (!context.performed)
                return;

            // Same key as typing "i" in inventory search (e.g. "giant"); do not close the panel while search focus is on.
            if (InventoryUI.IsOpenInSearchFocus())
                return;

            if (commandProcessor.IsPendingBowOrInventoryTargeting)
                return;

            InventoryUI.TogglePanelFromGameplayInput();
        }

        void OnAimBowPerformed(InputAction.CallbackContext context)
        {
            if (!context.performed)
                return;

            if (BlocksFloorGameplay())
                return;

            commandProcessor.TryApply(PlayerCommand.AimBow());
        }

        void OnOpenDoorPerformed(InputAction.CallbackContext context)
        {
            if (!context.performed || BlocksFloorGameplay())
                return;

            commandProcessor.TryApply(PlayerCommand.OpenDoor());
        }

        void OnCloseDoorPerformed(InputAction.CallbackContext context)
        {
            if (!context.performed || BlocksFloorGameplay())
                return;

            commandProcessor.TryApply(PlayerCommand.CloseDoor());
        }

        public bool TryBeginBowAim(
            BaseActor activeMember,
            ItemInstance restoreOffHandOnCancel,
            int inventoryResumeIndex) =>
            commandProcessor.TryBeginBowAim(activeMember, restoreOffHandOnCancel, inventoryResumeIndex);

        public bool TryBeginInventoryTargetedUse(
            BaseActor activeMember,
            AbilityAction ability,
            ItemInstance itemInstance,
            BaseActor itemOwner,
            int resumeSelectionIndex,
            string logTag) =>
            commandProcessor.TryBeginInventoryTargetedUse(
                activeMember,
                ability,
                itemInstance,
                itemOwner,
                resumeSelectionIndex,
                logTag);

        /// <summary>
        /// Apply a command from a recording or test harness. Same rules as live input (player turn, etc.).
        /// </summary>
        public bool TryApplyRecordedCommand(PlayerCommand command) => commandProcessor.TryApply(command);

        public void OnMove(InputAction.CallbackContext context)
        {
            if (BlocksFloorGameplay()) return;
            if (IsContextInvalid(context)) return;

            Vector2 input = context.ReadValue<Vector2>();
            Vector3Int direction = new Vector3Int(Mathf.RoundToInt(input.x), Mathf.RoundToInt(input.y), 0);
            commandProcessor.TryApply(PlayerCommand.MoveGrid(direction));
        }

        public void OnWait(InputAction.CallbackContext context)
        {
            if (BlocksFloorGameplay()) return;
            if (IsContextInvalid(context)) return;

            bool partyWait = Keyboard.current != null && Keyboard.current.shiftKey.isPressed;
            commandProcessor.TryApply(PlayerCommand.Wait(partyWait));
        }

        void OnRestPerformed(InputAction.CallbackContext context)
        {
            if (!context.performed)
                return;

            if (BlocksFloorGameplay() || RestSessionService.IsResting)
                return;

            if (IsContextInvalid(context))
                return;

            if (commandProcessor.CurrentState == InputState.Targeting)
                return;

            RestSessionService.TryStartOrDeny();
        }

        public void OnRest(InputAction.CallbackContext context) => OnRestPerformed(context);

        public void OnConfirm(InputAction.CallbackContext context)
        {
            if (!context.performed || commandProcessor.CurrentState != InputState.Targeting) return;
            commandProcessor.TryApply(PlayerCommand.ConfirmTarget());
        }

        public void OnCancel(InputAction.CallbackContext context)
        {
            if (!context.performed || commandProcessor.CurrentState != InputState.Targeting) return;
            commandProcessor.TryApply(PlayerCommand.CancelTarget());
        }

        public void OnToggleFormation(InputAction.CallbackContext context)
        {
            if (BlocksFloorGameplay()) return;
            if (IsContextInvalid(context)) return;
            commandProcessor.TryApply(PlayerCommand.ToggleFormation());
        }

        private void OnAbilityPerformed(InputAction.CallbackContext context, bool isShift, bool isCtrl)
        {
            if (BlocksFloorGameplay()) return;
            if (IsContextInvalid(context)) return;

            string keyName = context.control.name;
            if (!int.TryParse(keyName, out int numberPressed)) return;

            int slotIndex = numberPressed - 1;
            commandProcessor.TryApply(PlayerCommand.AbilitySlot(slotIndex, isShift, isCtrl));
        }

        private void SwapTo(InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            if (BlocksFloorGameplay()) return;

            string keyName = context.control.name;
            string suffix = keyName.Replace("F", "").Replace("f", "");
            if (!int.TryParse(suffix, out int numberPressed))
            {
                Debug.LogWarning($"Unrecognized key for party swap: {keyName}");
                return;
            }

            int index = numberPressed - 1;
            commandProcessor.TryApply(PlayerCommand.SwapPartyMember(index));
        }

        // Thin wrapper kept so existing tests can drive the rush via reflection
        // on this private name. The actual algorithm lives in FormationRushService.
        private void ProcessFollowerRush()
        {
            commandProcessor.ProcessFollowerRush();
        }

        static bool BlocksFloorGameplay() =>
            RestSessionService.IsResting
            || GameOverModalUI.BlocksGameplay
            || InventoryUI.BlocksGameplay || AutoPickupConfirmDialogUI.BlocksGameplay
            || TrapConfirmDialogUI.BlocksGameplay
            || HazardConfirmDialogUI.BlocksGameplay
            || FloorPickupMenuUI.BlocksGameplay
            || PartyMemberDeathDialogUI.BlocksGameplay
            || DungeonEndedDialogUI.BlocksGameplay
            || EnterDungeonDialogUI.BlocksGameplay
            || AdjacentInteractPickerModalUI.BlocksGameplay
            || AltarOfferingModalUI.BlocksGameplay
            || AltarUsedModalUI.BlocksGameplay
            || EssencePickupConfirmDialogUI.BlocksGameplay;

        private bool IsContextInvalid(InputAction.CallbackContext context) =>
            !context.performed
            || JRogue.World.Generation.DungeonExitService.ExitScheduled
            || JRogue.World.Generation.DungeonEntryService.EntryScheduled
            || DungeonEndedDialogUI.BlocksGameplay
            || EnterDungeonDialogUI.BlocksGameplay
            || TurnManager.Instance == null
            || TurnManager.Instance.currentState != GameState.PLAYER_TURN
            || GameOverModalUI.BlocksGameplay;
    }
}
