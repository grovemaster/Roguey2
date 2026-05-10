using JRogue.Item;
using JRogue.Item.World;
using JRogue.Manager.Equipment;
using JRogue.Manager.Inventory;
using UnityEngine;

namespace JRogue.Stats
{
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

            ItemInstance inst = groundItem.CollectInstance();
            if (inst == null || inst.Definition == null)
                return;

            if (inventory == null || !inventory.AddItem(inst))
                return;

            if (equipment != null
                && inst.Definition.damageModules != null
                && inst.Definition.damageModules.Count > 0)
            {
                equipment.EquipItem(EquipmentSlot.MainHand, inst);
            }

            Destroy(other.gameObject);
        }
    }
}