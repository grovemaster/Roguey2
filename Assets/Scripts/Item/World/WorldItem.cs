using JRogue.Manager.Equipment;
using JRogue.Manager.Inventory;
using UnityEngine;

namespace JRogue.Item.World
{
    /**
    A "World" item is visible on the world, typically meaning it can be picked up by the player
    */
    [RequireComponent(typeof(SpriteRenderer))]
    public class WorldItem : MonoBehaviour
    {
        public ItemData data; // Drag your Giants_Blade here // Drag your ScriptableObject (Sword, Potion) here
        private SpriteRenderer sr;

        // private void Awake()
        // {
        //     sr = GetComponent<SpriteRenderer>();
        // }

        private void Start()
        {
            // Set the world sprite to match the item's icon automatically
            if (data != null && data.icon != null)
            {
                //sr.sprite = data.icon;
                GetComponent<SpriteRenderer>().sprite = data.icon;
            }
        }

        // public void PickUp(InventoryManager inventory)
        // {
        //     // AddItem returns true if the player isn't too heavy
        //     if (inventory.AddItem(data))
        //     {
        //         Debug.Log($"You picked up: {data.itemName}");
        //         Destroy(gameObject); // Remove from the map
        //     }
        // }

        // This is where AddItem gets called!
        // public void PickUp(GameObject player)
        // {
        //     InventoryManager inventory = player.GetComponent<InventoryManager>();

        //     if (inventory != null)
        //     {
        //         // We call AddItem here. If it returns true (not too heavy), we destroy the floor object.
        //         if (inventory.AddItem(data))
        //         {
        //             Destroy(gameObject);
        //         }
        //     }
        // }

        /// <summary>Creates a new runtime <see cref="ItemInstance"/> (distinct id) from this world pick-up's <see cref="data"/>.</summary>
        public ItemInstance CollectInstance()
        {
            if (data == null)
                return null;

            var inst = ItemInstance.CreateFromDefinition(data);
            inst.StorageLocation = ItemStorageLocation.OnGround;
            return inst;
        }

        // public void PickUp(GameObject player)
        // {
        //     InventoryManager inventory = player.GetComponent<InventoryManager>();
        //     ItemData inventoryItemDataInstance = Instantiate(data);

        //     // 1. Try to add to inventory (this checks Weight/Encumbrance)
        //     if (inventory != null && inventory.AddItem(inventoryItemDataInstance))
        //     {
        //         // 2. Success! The item was added. 
        //         // Optional: For now, let's auto-equip it to see the stat boost
        //         EquipmentManager equipment = player.GetComponent<EquipmentManager>();
        //         if (equipment != null && inventoryItemDataInstance.damageModules.Count > 0)
        //         {
        //             equipment.EquipWeapon(inventoryItemDataInstance);
        //         }

        //         // 3. Remove the object from the physical world
        //         Destroy(gameObject);
        //     }
        //     else
        //     {
        //         // If inventory.AddItem returned false, the player is too heavy!
        //         Debug.LogWarning("You are too encumbered to pick this up!");
        //     }
        // }
    }
}