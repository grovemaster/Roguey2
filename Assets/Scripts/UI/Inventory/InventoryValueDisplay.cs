using System.Globalization;
using JRogue.Item;

namespace JRogue.UI.Inventory
{
    /// <summary>Formats gold / appraisal display for list and inspect panes.</summary>
    public static class InventoryValueDisplay
    {
        public const string Unknown = "?";
        public const string NoValue = "—";

        public static string FormatListColumn(ItemInstance instance, ItemData definition)
        {
            if (definition == null || !definition.HasMonetaryValue)
                return NoValue;

            if (definition.requiresAppraisal && (instance == null || !instance.IsAppraised))
                return Unknown;

            int stack = instance != null ? instance.Quantity : 1;
            int total = definition.goldValue * stack;
            if (total <= 0 && !definition.requiresAppraisal)
                return NoValue;

            return FormatGold(total);
        }

        public static string FormatInspectValue(ItemInstance instance, ItemData definition, bool richText)
        {
            string raw = FormatListColumn(instance, definition);
            if (raw == Unknown)
            {
                return richText
                    ? $"<color=#8a97a3>Value (stack):</color> <color=#6a7884>{Unknown}</color>  <color=#5a6974>(unappraised)</color>"
                    : $"{Unknown} (unappraised)";
            }

            if (raw == NoValue)
                return richText ? "<color=#8a97a3>Value (stack):</color> —" : NoValue;

            return richText
                ? $"<color=#8a97a3>Value (stack):</color> {raw}"
                : raw;
        }

        public static string FormatGold(int amount) =>
            amount.ToString("N0", CultureInfo.InvariantCulture);
    }
}
