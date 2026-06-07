using System.Linq;
using JRogue.Actors;
using JRogue.Item;
using JRogue.Manager.Combat;
using JRogue.Manager.Equipment;
using JRogue.Manager.Inventory;
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
            out string message)
        {
            message = null;

            if (instance == null || from == null || to == null)
            {
                message = "Invalid transfer.";
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

            if (!toInventory.CanCarry(instance))
            {
                message = $"{to.DisplayName} cannot carry {instance.Definition?.itemName}.";
                return false;
            }

            if (!fromInventory.TryRemoveCarried(instance))
            {
                message = "Could not remove item from giver.";
                return false;
            }

            if (!toInventory.AddItem(instance))
            {
                fromInventory.AddItem(instance);
                message = "Recipient could not receive the item.";
                return false;
            }

            ClearHotbarReferences(from, instance.Id);
            message = $"Gave {instance.Definition?.itemName} to {to.DisplayName}.";
            Debug.Log($"{LogPrefix} {message}");
            return true;
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
