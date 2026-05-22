using JRogue.Ability;
using JRogue.Item;
using JRogue.Manager.Inventory;
using JRogue.UI.Inventory;
using UnityEngine;

namespace JRogue.Manager.Inventory
{
    /// <summary>Consumes / activates items from a party member's carried inventory.</summary>
    public static class InventoryItemUse
    {
        public static bool TryUseCarriedItem(InventoryViewModel.Row row, bool inCombat, out string failureReason)
        {
            failureReason = null;

            if (row.Owner == null || row.Item == null)
            {
                failureReason = "Invalid item or owner.";
                return false;
            }

            if (row.Instance != null && row.Instance.StorageLocation == ItemStorageLocation.OnGround)
            {
                failureReason = "Pick up the item before using it.";
                return false;
            }

            if (!InventoryUsability.AppearsUsableNow(row, inCombat))
            {
                if (!InventoryConsumePolicy.CanConsume(row, out failureReason))
                    return false;
                failureReason ??= "Cannot use this item right now.";
                return false;
            }

            if (!InventoryConsumePolicy.CanConsume(row, out failureReason))
                return false;

            if (row.Item.activeAbilities == null || row.Item.activeAbilities.Count == 0)
            {
                failureReason = "Item has no active ability.";
                return false;
            }

            AbilityAction ability = row.Item.activeAbilities[0];
            if (ability == null)
            {
                failureReason = "Item ability is missing.";
                return false;
            }

            if (!ability.CanExecute(row.Owner.gameObject))
            {
                failureReason = "Cannot use this item right now.";
                return false;
            }

            if (!ability.Execute(row.Owner.gameObject))
            {
                failureReason = "Item use failed.";
                return false;
            }

            InventoryManager inventory = row.Owner.GetComponent<InventoryManager>();
            if (inventory != null && row.Instance != null)
                inventory.TryRemoveCarried(row.Instance);

            return true;
        }
    }
}
