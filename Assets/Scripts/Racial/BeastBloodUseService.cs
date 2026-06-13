using JRogue.Actors;
using JRogue.Item;
using JRogue.Manager.Inventory;
using JRogue.UI.Inventory;
using UnityEngine;

namespace JRogue.Racial
{
    public static class SoulBeastLevelService
    {
        public static bool TryUseBeastBlood(BaseActor target, out string failureReason, out int newLevel)
        {
            newLevel = 0;
            failureReason = null;

            if (!SoulBeastPartyRules.IsEligibleBeastman(target, requireUnbonded: false, out failureReason))
                return false;

            BeastmanSoulBeastRuntime runtime = target.GetComponent<BeastmanSoulBeastRuntime>();
            if (runtime == null)
            {
                failureReason = "No Soul Beast runtime.";
                return false;
            }

            if (!runtime.TryResolveBondedDefinition(out SoulBeastDefinition beast))
            {
                failureReason = "Unknown Soul Beast contract.";
                return false;
            }

            int cap = SoulBeastProgressionLogic.GetEffectiveLevelCap(target.stats, beast);
            if (runtime.SoulBeastLevel >= cap)
            {
                failureReason = $"Soul Beast level cannot exceed {target.DisplayName}'s level ({cap}).";
                return false;
            }

            if (!runtime.TryIncrementLevel(out failureReason))
                return false;

            newLevel = runtime.SoulBeastLevel;
            return true;
        }
    }

    public static class BeastBloodUseService
    {
        static System.Action _inventoryRefreshCallback;

        public static void SetInventoryRefreshCallback(System.Action callback) =>
            _inventoryRefreshCallback = callback;

        static void NotifyInventoryChanged()
        {
            System.Action callback = _inventoryRefreshCallback;
            _inventoryRefreshCallback = null;
            callback?.Invoke();
        }

        public static InventoryUseResult TryBeginUse(InventoryViewModel.Row row)
        {
            if (row.Owner == null || row.Instance == null || row.Item == null)
                return InventoryUseResult.Fail("Invalid item or owner.");

            if (row.Item is not BeastBloodItemData)
                return InventoryUseResult.Fail("Not Beast Blood.");

            if (!SoulBeastPartyRules.CanUseBeastBlood(out string rejectReason))
                return InventoryUseResult.Fail(rejectReason ?? "Cannot use Beast Blood right now.");

            BaseActor target = SoulBeastPartyRules.FindBondedBeastmanForBloodUse(out _);
            if (target == null)
                return InventoryUseResult.Fail("Requires a Beastman bonded to a Soul Beast.");

            InventoryManager inventory = row.Owner.GetComponent<InventoryManager>();
            if (inventory == null || !inventory.TryConsumeCarriedQuantity(row.Instance, 1))
                return InventoryUseResult.Fail("Beast Blood could not be consumed.");

            NotifyInventoryChanged();

            if (!SoulBeastLevelService.TryUseBeastBlood(target, out string failureReason, out int newLevel))
            {
                ShowFeedback(failureReason ?? "Beast Blood had no effect.");
                return InventoryUseResult.Fail(failureReason ?? "Beast Blood had no effect.");
            }

            BeastmanSoulBeastRuntime runtime = target.GetComponent<BeastmanSoulBeastRuntime>();
            if (runtime != null && runtime.TryResolveBondedDefinition(out SoulBeastDefinition beast))
            {
                string beastName = string.IsNullOrWhiteSpace(beast.displayName)
                    ? beast.soulBeastId
                    : beast.displayName.Trim();
                ShowFeedback($"{target.DisplayName}'s Soul Beast {beastName} reaches level {newLevel}.");
            }
            else
            {
                ShowFeedback($"{target.DisplayName}'s Soul Beast reaches level {newLevel}.");
            }

            return InventoryUseResult.Consumed();
        }

        static void ShowFeedback(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return;

            Debug.Log($"[BeastBlood] {line}");
        }
    }
}
