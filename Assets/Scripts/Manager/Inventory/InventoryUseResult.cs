using JRogue.Ability;
using JRogue.Actors;
using JRogue.Item;

namespace JRogue.Manager.Inventory
{
    public enum InventoryUseOutcome
    {
        Failed = 0,
        ConsumedImmediately = 1,
        StartedTargeting = 2,
        StartedBowAim = 3,
        StartedChoiceDialog = 4,
    }

    /// <summary>Captured when <see cref="InventoryUseOutcome.StartedTargeting"/> — confirm consumes, cancel restores UI.</summary>
    public readonly struct InventoryTargetedUsePending
    {
        public AbilityAction Ability { get; }
        public ItemInstance Instance { get; }
        public BaseActor Owner { get; }
        public int ResumeSelectionIndex { get; }
        public string LogTag { get; }

        public InventoryTargetedUsePending(
            AbilityAction ability,
            ItemInstance instance,
            BaseActor owner,
            int resumeSelectionIndex,
            string logTag)
        {
            Ability = ability;
            Instance = instance;
            Owner = owner;
            ResumeSelectionIndex = resumeSelectionIndex;
            LogTag = logTag ?? string.Empty;
        }
    }

    /// <summary>Invoke arrow from inventory while bow equipped.</summary>
    public readonly struct InventoryBowAimPending
    {
        public BaseActor Owner { get; }
        public ItemInstance InvokeArrowInstance { get; }
        public ItemInstance RestoreOffHandAfterCancel { get; }
        public int ResumeSelectionIndex { get; }

        public InventoryBowAimPending(
            BaseActor owner,
            ItemInstance invokeArrowInstance,
            ItemInstance restoreOffHandAfterCancel,
            int resumeSelectionIndex)
        {
            Owner = owner;
            InvokeArrowInstance = invokeArrowInstance;
            RestoreOffHandAfterCancel = restoreOffHandAfterCancel;
            ResumeSelectionIndex = resumeSelectionIndex;
        }
    }

    public readonly struct InventoryUseResult
    {
        public InventoryUseOutcome Outcome { get; }
        public string FailureReason { get; }
        public InventoryTargetedUsePending TargetingPending { get; }
        public InventoryBowAimPending BowAimPending { get; }

        InventoryUseResult(
            InventoryUseOutcome outcome,
            string failureReason,
            InventoryTargetedUsePending targetingPending,
            InventoryBowAimPending bowAimPending)
        {
            Outcome = outcome;
            FailureReason = failureReason;
            TargetingPending = targetingPending;
            BowAimPending = bowAimPending;
        }

        public static InventoryUseResult Fail(string reason) =>
            new InventoryUseResult(InventoryUseOutcome.Failed, reason, default, default);

        public static InventoryUseResult Consumed() =>
            new InventoryUseResult(InventoryUseOutcome.ConsumedImmediately, null, default, default);

        public static InventoryUseResult StartTargeting(InventoryTargetedUsePending pending) =>
            new InventoryUseResult(InventoryUseOutcome.StartedTargeting, null, pending, default);

        public static InventoryUseResult StartBowAim(InventoryBowAimPending pending) =>
            new InventoryUseResult(InventoryUseOutcome.StartedBowAim, null, default, pending);

        public static InventoryUseResult StartChoiceDialog() =>
            new InventoryUseResult(InventoryUseOutcome.StartedChoiceDialog, null, default, default);
    }
}
