using JRogue.Item;
using JRogue.Item.World;
using JRogue.Manager.Equipment;
using JRogue.Manager.Inventory;
using UnityEngine;

namespace JRogue.Stats
{
    /// <summary>
    /// Picks up scene-placed <see cref="WorldItem"/> objects (e.g. swords) on trigger overlap.
    /// Mana stones dropped from enemies use <see cref="JRogue.Manager.Loot.ManaStoneAutoPickupService"/> instead.
    /// </summary>
    public class InventoryCollector : MonoBehaviour
    {
        InventoryManager inventory;
        EquipmentManager equipment;

        void Awake()
        {
            inventory = GetComponent<InventoryManager>();
            equipment = GetComponent<EquipmentManager>();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.TryGetComponent(out WorldItem groundItem))
                return;

            TryCollectWorldItem(groundItem, gameObject, allowConfirmGated: false);
        }

        /// <summary>Grid movement may not always fire triggers; called from tile-enter pickup as well.</summary>
        internal static bool TryCollectWorldItem(WorldItem groundItem, GameObject picker, bool allowConfirmGated = false)
        {
            if (groundItem == null || picker == null)
                return false;

            if (groundItem.data is ManaStoneItemData)
                return false;

            if (groundItem.data != null)
            {
                if (!groundItem.data.autoPickupOnStep)
                    return false;

                if (groundItem.data.RequiresConfirmBeforeAutoPickupOnStep && !allowConfirmGated)
                    return false;
            }

            ItemInstance inst = groundItem.CollectInstance();
            if (inst == null || inst.Definition == null)
                return false;

            InventoryManager inventory = picker.GetComponent<InventoryManager>();
            if (inventory == null || !inventory.AddItem(inst))
                return false;

            EquipmentManager equipment = picker.GetComponent<EquipmentManager>();
            if (equipment != null
                && inst.Definition.damageModules != null
                && inst.Definition.damageModules.Count > 0
                && EquipmentLegalityEvaluator.CanEquip(picker, inst.Definition, EquipmentSlot.MainHand,
                    out _))
            {
                equipment.EquipItem(EquipmentSlot.MainHand, inst);
            }

            Object.Destroy(groundItem.gameObject);
            return true;
        }
    }
}
