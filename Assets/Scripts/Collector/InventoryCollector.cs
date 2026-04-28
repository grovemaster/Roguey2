using JRogue.Item;
using JRogue.Item.World;
using JRogue.Manager.Equipment;
using JRogue.Manager.Inventory;
using UnityEngine;

namespace JRogue.Stats
{
    public class InventoryCollector : MonoBehaviour
    {
        private InventoryManager inventory;
        private EquipmentManager equipment;

        void Awake()
        {
            inventory = GetComponent<InventoryManager>();
            equipment = GetComponent<EquipmentManager>();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // 1. Check if the object is even a WorldItem
            if (other.TryGetComponent(out WorldItem groundItem))
            {
                // 2. Get the unique data instance
                ItemData instance = groundItem.Collect();

                // 3. Attempt to add to this specific entity's inventory
                if (inventory != null && inventory.AddItem(instance))
                {
                    // 4. Auto-equip if it's a weapon (Matches your current logic)
                    if (equipment != null && instance.damageModules.Count > 0)
                    {
                        equipment.EquipItem(EquipmentSlot.MainHand, instance);
                        // equipment.EquipWeapon(instance);
                    }

                    Destroy(other.gameObject);
                }
            }
        }
    }
}