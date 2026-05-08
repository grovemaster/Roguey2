using JRogue.Manager.Turn;
using JRogue.UI.Targeting;
using UnityEngine;
using UnityEngine.InputSystem;

namespace JRogue.Input
{
    public class InputHandler : MonoBehaviour
    {
        private GameControls controls;
        private readonly PlayerCommandProcessor commandProcessor = new PlayerCommandProcessor();

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
        }

        private void OnEnable() => controls.Player.Enable();
        private void OnDisable() => controls.Player.Disable();

        private void OnDestroy()
        {
            controls?.Dispose();
        }

        /// <summary>
        /// Apply a command from a recording or test harness. Same rules as live input (player turn, etc.).
        /// </summary>
        public bool TryApplyRecordedCommand(PlayerCommand command) => commandProcessor.TryApply(command);

        public void OnMove(InputAction.CallbackContext context)
        {
            if (IsContextInvalid(context)) return;

            Vector2 input = context.ReadValue<Vector2>();
            Vector3Int direction = new Vector3Int(Mathf.RoundToInt(input.x), Mathf.RoundToInt(input.y), 0);
            commandProcessor.TryApply(PlayerCommand.MoveGrid(direction));
        }

        public void OnWait(InputAction.CallbackContext context)
        {
            if (IsContextInvalid(context)) return;

            bool partyWait = Keyboard.current != null && Keyboard.current.shiftKey.isPressed;
            commandProcessor.TryApply(PlayerCommand.Wait(partyWait));
        }

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
            if (IsContextInvalid(context)) return;
            commandProcessor.TryApply(PlayerCommand.ToggleFormation());
        }

        private void OnAbilityPerformed(InputAction.CallbackContext context, bool isShift, bool isCtrl)
        {
            if (IsContextInvalid(context)) return;

            string keyName = context.control.name;
            if (!int.TryParse(keyName, out int numberPressed)) return;

            int slotIndex = numberPressed - 1;
            commandProcessor.TryApply(PlayerCommand.AbilitySlot(slotIndex, isShift, isCtrl));
        }

        private void SwapTo(InputAction.CallbackContext context)
        {
            if (!context.performed) return;

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

        private bool IsContextInvalid(InputAction.CallbackContext context) =>
            !context.performed
            || TurnManager.Instance == null
            || TurnManager.Instance.currentState != GameState.PLAYER_TURN;
    }
}
