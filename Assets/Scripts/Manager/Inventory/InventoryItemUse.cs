using JRogue.Ability;
using JRogue.Actors;
using JRogue.Item;
using JRogue.Manager.Party;
using JRogue.Manager.Turn;
using JRogue.UI.Inventory;
using UnityEngine;

namespace JRogue.Manager.Inventory
{
    /// <summary>Consumes / activates items from a party member's carried inventory.</summary>
    public static class InventoryItemUse
    {
        public static InventoryUseResult TryUseCarriedItem(InventoryViewModel.Row row, bool inCombat)
        {
            if (row.Owner == null || row.Item == null)
                return InventoryUseResult.Fail("Invalid item or owner.");

            if (row.Instance != null && row.Instance.StorageLocation == ItemStorageLocation.OnGround)
                return InventoryUseResult.Fail("Pick up the item before using it.");

            if (!InventoryUsability.AppearsUsableNow(row, inCombat))
            {
                if (!InventoryConsumePolicy.CanConsume(row, out string reason))
                    return InventoryUseResult.Fail(reason);
                return InventoryUseResult.Fail("Cannot use this item right now.");
            }

            if (!InventoryConsumePolicy.CanConsume(row, out string consumeReason))
                return InventoryUseResult.Fail(consumeReason);

            TurnManager turnManager = TurnManager.Instance;
            if (turnManager == null || turnManager.currentState != GameState.PLAYER_TURN)
                return InventoryUseResult.Fail("Not your turn.");

            PartyManager party = PartyManager.Instance;
            BaseActor activeMember = party != null ? party.GetActiveMember() : null;
            if (activeMember == null || !turnManager.CanActorTakeAction(activeMember.gameObject))
                return InventoryUseResult.Fail("Already acted this turn.");

            if (row.Item.activeAbilities == null || row.Item.activeAbilities.Count == 0)
                return InventoryUseResult.Fail("Item has no active ability.");

            AbilityAction ability = row.Item.activeAbilities[0];
            if (ability == null)
                return InventoryUseResult.Fail("Item ability is missing.");

            if (!ability.CanExecute(row.Owner.gameObject))
                return InventoryUseResult.Fail("Cannot use this item right now.");

            string logTag = row.Item.inventoryTargetedUseLogTag;

            if (ability.requiresTarget)
            {
                var pending = new InventoryTargetedUsePending(
                    ability,
                    row.Instance,
                    row.Owner,
                    resumeSelectionIndex: 0,
                    logTag);
                return InventoryUseResult.StartTargeting(pending);
            }

            if (!ability.Execute(row.Owner.gameObject))
                return InventoryUseResult.Fail("Item use failed.");

            InventoryManager inventory = row.Owner.GetComponent<InventoryManager>();
            if (inventory != null && row.Instance != null)
                inventory.TryConsumeCarriedQuantity(row.Instance, 1);

            return InventoryUseResult.Consumed();
        }
    }
}
