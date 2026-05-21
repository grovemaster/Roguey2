using JRogue.Item;
using JRogue.Stats;
using JRogue.UI.Inventory;

namespace JRogue.Manager.Inventory
{
    public static class InventoryConsumePolicy
    {
        public const string UndeadPotionBanMessage = "Undead cannot drink potions.";

        public static bool CanConsume(InventoryViewModel.Row row, out string failureReason)
        {
            failureReason = null;
            if (row.Item == null || row.Owner == null)
            {
                failureReason = "Invalid item or owner.";
                return false;
            }

            if (row.Item.category == ItemCategory.Potion &&
                row.Owner.TryGetComponent(out CharacterStats stats) &&
                stats.race == Race.Undead)
            {
                failureReason = UndeadPotionBanMessage;
                return false;
            }

            return true;
        }
    }
}
