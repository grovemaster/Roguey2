using JRogue.Ability;
using JRogue.Manager.Inventory;
using UnityEngine;

namespace JRogue.Item
{
    public static class EvocableChargeRules
    {
        public const string LogPrefix = "[Evocable]";

        public static bool IsEvocable(ItemInstance instance) =>
            instance?.Definition is EvocableItemData;

        public static EvocableItemData AsDefinition(ItemData definition) =>
            definition as EvocableItemData;

        public static AbilityAction GetInvokeAbility(ItemData definition)
        {
            if (definition is EvocableItemData evocable && evocable.invokeAbility != null)
                return evocable.invokeAbility;
            return null;
        }

        public static void InitializeCharges(ItemInstance instance, EvocableItemData definition, int? startingOverride = null)
        {
            if (instance == null || definition == null)
                return;

            instance.Quantity = 1;
            int max = Mathf.Max(1, definition.maxCharges);
            int start = Mathf.Clamp(startingOverride ?? definition.startingCharges, 0, max);
            instance.SetCharges(start, max);
            instance.RechargePhasesAccumulated = 0;
        }

        public static void ClampCharges(ItemInstance instance)
        {
            if (instance == null)
                return;

            instance.Quantity = 1;
            if (instance.MaxCharges < 1)
                instance.MaxCharges = 1;
            instance.CurrentCharges = Mathf.Clamp(instance.CurrentCharges, 0, instance.MaxCharges);
        }

        public static bool HasChargeToInvoke(ItemInstance instance) =>
            instance != null && instance.CurrentCharges > 0;

        /// <summary>Call only after <see cref="AbilityAction.Execute"/> returned true.</summary>
        public static void SpendChargeAfterSuccessfulInvoke(InventoryManager inventory, ItemInstance instance)
        {
            if (instance == null || inventory == null)
                return;

            if (instance.Definition is not EvocableItemData definition)
                return;

            if (instance.CurrentCharges <= 0)
            {
                Debug.LogWarning($"{LogPrefix} SpendCharge called with 0 charges id={instance.Id}.");
                return;
            }

            int before = instance.CurrentCharges;
            instance.CurrentCharges = before - 1;
            ClampCharges(instance);

            Debug.Log(
                $"{LogPrefix} Invoke success id={ShortId(instance)} charges {before}->{instance.CurrentCharges}/{instance.MaxCharges}.");

            if (instance.CurrentCharges > 0)
                return;

            if (definition.consumesWhenEmpty)
            {
                inventory.TryRemoveCarried(instance);
                Debug.Log($"{LogPrefix} Removed at 0 charges id={ShortId(instance)} ({definition.itemName}).");
                return;
            }

            instance.RechargePhasesAccumulated = 0;
            Debug.Log(
                $"{LogPrefix} Depleted; recharging every {definition.rechargeIntervalPlayerPhases} player phases id={ShortId(instance)}.");
        }

        public static string FormatChargeColumn(ItemInstance instance, ItemData definition)
        {
            if (definition is not EvocableItemData)
                return null;

            if (instance == null)
                return $"?/{Mathf.Max(1, ((EvocableItemData)definition).maxCharges)}";

            ClampCharges(instance);
            return $"{instance.CurrentCharges}/{instance.MaxCharges}";
        }

        public static string FormatRechargeSubtitle(ItemInstance instance, EvocableItemData definition)
        {
            if (instance == null || definition == null || definition.consumesWhenEmpty)
                return null;

            if (instance.CurrentCharges > 0)
                return null;

            int interval = Mathf.Max(1, definition.rechargeIntervalPlayerPhases);
            int remaining = interval - instance.RechargePhasesAccumulated;
            if (remaining < 1)
                remaining = interval;
            return $"Recharging ({remaining} phases)";
        }

        static string ShortId(ItemInstance instance) =>
            instance != null && instance.Id != null && instance.Id.Length >= 6
                ? instance.Id.Substring(0, 6)
                : "?";
    }
}
