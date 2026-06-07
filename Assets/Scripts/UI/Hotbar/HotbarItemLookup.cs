using JRogue.Actors;
using JRogue.Item;
using JRogue.Manager.Equipment;
using JRogue.Manager.Inventory;

namespace JRogue.UI.Hotbar
{
    internal static class HotbarItemLookup
    {
        public static bool TryFindOwnedItem(
            BaseActor actor,
            string itemInstanceId,
            out ItemInstance instance,
            out BaseActor owner,
            out bool isEquipped,
            out EquipmentSlot? equippedSlot)
        {
            instance = null;
            owner = null;
            isEquipped = false;
            equippedSlot = null;

            if (actor == null || string.IsNullOrEmpty(itemInstanceId))
                return false;

            if (TryFindInActor(actor, itemInstanceId, out instance, out isEquipped, out equippedSlot))
            {
                owner = actor;
                return true;
            }

            return false;
        }

        static bool TryFindInActor(
            BaseActor actor,
            string itemInstanceId,
            out ItemInstance instance,
            out bool isEquipped,
            out EquipmentSlot? equippedSlot)
        {
            instance = null;
            isEquipped = false;
            equippedSlot = null;

            InventoryManager inventory = actor.GetComponent<InventoryManager>();
            if (inventory != null)
            {
                foreach (ItemInstance carried in inventory.CarriedItems)
                {
                    if (carried != null && carried.Id == itemInstanceId)
                    {
                        instance = carried;
                        return true;
                    }
                }
            }

            EquipmentManager equipment = actor.GetComponent<EquipmentManager>();
            if (equipment != null)
            {
                foreach (var kv in equipment.EquippedSnapshot)
                {
                    ItemInstance equipped = kv.Value;
                    if (equipped != null && equipped.Id == itemInstanceId)
                    {
                        instance = equipped;
                        isEquipped = true;
                        equippedSlot = kv.Key;
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
