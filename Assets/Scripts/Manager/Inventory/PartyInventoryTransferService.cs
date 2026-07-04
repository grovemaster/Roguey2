using System.Linq;
using JRogue.Actors;
using JRogue.Item;
using JRogue.Manager.Combat;
using JRogue.Manager.Equipment;
using JRogue.Manager.Party;
using JRogue.UI.Hotbar;
using UnityEngine;

namespace JRogue.Manager.Inventory
{
    public static class PartyInventoryTransferService
    {
        const string LogPrefix = "[Inventory]";

        public static bool TryGiveCarriedItem(
            ItemInstance instance,
            BaseActor from,
            BaseActor to,
            int quantity,
            out string message)
        {
            message = null;

            if (instance == null || from == null || to == null)
            {
                message = "Invalid transfer.";
                return false;
            }

            if (quantity < 1)
            {
                message = "Invalid quantity.";
                return false;
            }

            if (quantity > instance.Quantity)
            {
                message = "Not enough items in stack.";
                return false;
            }

            if (CombatThreatCoordinator.Instance != null && CombatThreatCoordinator.Instance.IsInCombat)
            {
                message = $"{LogPrefix} Cannot transfer items during combat.";
                return false;
            }

            if (from == to)
            {
                message = "Cannot give an item to yourself.";
                return false;
            }

            PartyManager party = PartyManager.Instance;
            if (party == null || !party.partyMembers.Contains(from) || !party.partyMembers.Contains(to))
            {
                message = "Both characters must be in the party.";
                return false;
            }

            if (instance.Definition != null && instance.Definition.category == ItemCategory.Essence)
            {
                message = "Essences cannot be transferred.";
                return false;
            }

            EquipmentManager fromEquipment = from.GetComponent<EquipmentManager>();
            if (fromEquipment != null && fromEquipment.TryGetEquippedSlot(instance, out _))
            {
                message = "Unequip the item before giving it away.";
                return false;
            }

            InventoryManager fromInventory = from.GetComponent<InventoryManager>();
            InventoryManager toInventory = to.GetComponent<InventoryManager>();
            if (fromInventory == null || toInventory == null)
            {
                message = "Missing inventory.";
                return false;
            }

            if (!fromInventory.CarriedItems.Contains(instance))
            {
                message = "Item is not in the giver's carried inventory.";
                return false;
            }

            bool fullTransfer = quantity >= instance.Quantity;
            ItemInstance carryCheck = fullTransfer
                ? instance
                : CreateCarriedSplit(instance, quantity);

            if (!toInventory.CanCarry(carryCheck))
            {
                message = $"{to.DisplayName} cannot carry {instance.Definition?.itemName}.";
                return false;
            }

            if (fullTransfer)
            {
                if (!fromInventory.TryRemoveCarried(instance))
                {
                    message = "Could not remove item from giver.";
                    return false;
                }

                if (!toInventory.TryStowCarriedItem(instance))
                {
                    fromInventory.AddItem(instance);
                    message = "Recipient could not receive the item.";
                    return false;
                }

                ClearHotbarReferences(from, instance.Id);
                message = FormatSuccessMessage(instance.Definition?.itemName, to.DisplayName, quantity);
                Debug.Log($"{LogPrefix} {message}");
                return true;
            }

            if (!fromInventory.TryConsumeCarriedQuantity(instance, quantity))
            {
                message = "Could not remove item from giver.";
                return false;
            }

            ItemInstance split = CreateCarriedSplit(instance, quantity);
            if (!toInventory.TryStowCarriedItem(split))
            {
                instance.Quantity += quantity;
                message = "Recipient could not receive the item.";
                return false;
            }

            message = FormatSuccessMessage(instance.Definition?.itemName, to.DisplayName, quantity);
            Debug.Log($"{LogPrefix} {message}");
            return true;
        }

        static string FormatSuccessMessage(string itemName, string recipientName, int quantity)
        {
            string label = string.IsNullOrEmpty(itemName) ? "item" : itemName;
            return quantity > 1
                ? $"Gave {quantity} × {label} to {recipientName}."
                : $"Gave {label} to {recipientName}.";
        }

        static ItemInstance CreateCarriedSplit(ItemInstance source, int quantity)
        {
            var split = new ItemInstance(source.Definition, quantity)
            {
                IsAppraised = source.IsAppraised,
                UserMarks = source.UserMarks,
                UserInscription = source.UserInscription,
                StorageLocation = ItemStorageLocation.Carried,
            };
            return split;
        }

        static void ClearHotbarReferences(BaseActor from, string itemInstanceId)
        {
            if (from == null || string.IsNullOrEmpty(itemInstanceId))
                return;

            HotbarLayout layout = from.GetComponent<HotbarLayout>();
            if (layout == null)
                return;

            for (int i = 0; i < HotbarLayout.HotbarMainSlotCount; i++)
            {
                HotbarEntry entry = layout.GetSlot(i);
                if (entry.IsEmpty())
                    continue;

                if (entry.itemInstanceId == itemInstanceId)
                    layout.SetSlot(i, new HotbarEntry());
            }
        }
    }
}
