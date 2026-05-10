using System;
using JRogue.Item;
using UnityEngine;

namespace JRogue.Manager.Inventory
{
    /// <summary>Phase 3: optional rules applied when items enter a bag (<see cref="InventoryManager.AddItem"/>).</summary>
    [CreateAssetMenu(fileName = "InventoryAutomation", menuName = "JRogue/Inventory/Automation Profile")]
    public sealed class InventoryAutomationProfile : ScriptableObject
    {
        [Tooltip("Reorder carried list after each successful non-currency pickup.")]
        public bool sortCarriedAfterEveryPickup = true;

        [Tooltip("When pickup category matches, set <see cref=\"ItemUserMark.Junk\"/> on the new instance.")]
        public bool applyJunkMarkOnCategoryMatch = false;

        public ItemCategory[] autoJunkCategories = Array.Empty<ItemCategory>();

        [Tooltip("Future: collector / auto-pickup allow-list (not wired in Phase 3 pass).")]
        public ItemCategory[] autoPickupAllowedCategories = Array.Empty<ItemCategory>();

        public bool ShouldAutoJunk(ItemCategory category)
        {
            if (!applyJunkMarkOnCategoryMatch || autoJunkCategories == null)
                return false;

            foreach (ItemCategory c in autoJunkCategories)
            {
                if (c == category)
                    return true;
            }

            return false;
        }
    }
}
