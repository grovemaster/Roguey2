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

    public readonly struct InventoryUseResult
    {
        public InventoryUseOutcome Outcome { get; }
        public string FailureReason { get; }
        public InventoryTargetedUsePending TargetingPending { get; }

        InventoryUseResult(
            InventoryUseOutcome outcome,
            string failureReason,
            InventoryTargetedUsePending targetingPending)
        {
            Outcome = outcome;
            FailureReason = failureReason;
            TargetingPending = targetingPending;
        }

        public static InventoryUseResult Fail(string reason) =>
            new InventoryUseResult(InventoryUseOutcome.Failed, reason, default);

        public static InventoryUseResult Consumed() =>
            new InventoryUseResult(InventoryUseOutcome.ConsumedImmediately, null, default);

        public static InventoryUseResult StartTargeting(InventoryTargetedUsePending pending) =>
            new InventoryUseResult(InventoryUseOutcome.StartedTargeting, null, pending);
    }
}
