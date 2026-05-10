using JRogue.Manager.Inventory;
using JRogue.Item;

namespace JRogue.UI.Inventory
{
    /// <summary>Centralized &quot;can this appear as usable now?&quot; for filtered views.</summary>
    public static class InventoryUsability
    {
        public static bool AppearsUsableNow(InventoryViewModel.Row row, bool inCombat)
        {
            ItemData item = row.Item;
            if (item == null)
                return false;

            bool hasActiveAbility = item.activeAbilities != null && item.activeAbilities.Count > 0;

            switch (item.category)
            {
                case ItemCategory.Potion:
                case ItemCategory.Scroll:
                    if (row.IsEquipped)
                        return false;
                    if (!inCombat)
                        return row.Owner != null;
                    return row.Owner != null &&
                           InventoryPolicy.CanUseCarriedFromAlly(row.Owner, row.Owner, itemEquippedElsewhere: false);

                default:
                    if (!hasActiveAbility)
                        return false;
                    return !row.IsEquipped && row.Owner != null;
            }
        }
    }
}
