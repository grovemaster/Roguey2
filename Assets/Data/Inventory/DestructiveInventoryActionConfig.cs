using JRogue.Item;
using UnityEngine;

namespace JRogue.UI.Inventory
{
    [CreateAssetMenu(fileName = "DestructiveInventoryRules", menuName = "JRogue/Inventory/Destructive Action Confirm Rules")]
    public sealed class DestructiveInventoryActionConfig : ScriptableObject
    {
        [Tooltip("Drop always asks for confirmation when the item belongs to these categories.")]
        public ItemCategory[] confirmDropCategories = System.Array.Empty<ItemCategory>();

        [Tooltip("Confirm drop when ItemData.inventoryRiskHints shares any of these bits.")]
        public ItemInventoryRiskHint confirmDropIfItemHintsAny =
            ItemInventoryRiskHint.StoryTagged | ItemInventoryRiskHint.Rare | ItemInventoryRiskHint.Cursed |
            ItemInventoryRiskHint.HighValue;

        public bool ShouldConfirmDrop(ItemData item)
        {
            if (item == null)
                return false;

            if (confirmDropCategories != null)
            {
                foreach (ItemCategory c in confirmDropCategories)
                {
                    if (item.category == c)
                        return true;
                }
            }

            return (confirmDropIfItemHintsAny & item.inventoryRiskHints) != 0;
        }
    }
}
