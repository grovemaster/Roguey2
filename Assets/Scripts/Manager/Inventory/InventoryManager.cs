using UnityEngine;
using System.Collections.Generic;
using JRogue.Stats;
using System.Linq;
using JRogue.Item; // Added for easy Summing

namespace JRogue.Manager.Inventory
{
    public class InventoryManager : MonoBehaviour
    {
        public List<ItemData> items = new List<ItemData>();
        private CharacterStats stats;

        void Awake()
        {
            stats = GetComponent<CharacterStats>();
        }

        public float GetTotalWeight() => items.Sum(i => i.weight);

        public bool AddItem(ItemData item)
        {
            Debug.Log($"Attempting to add item {item.itemName}");
            if (CanCarry(item))
            {
                items.Add(item);
                Debug.Log($"Inventory: Added {item.itemName}. Current Weight: {GetTotalWeight()}/{stats.EncumbranceLimit}");
                return true;
            }

            Debug.LogWarning($"Too heavy! Cannot carry {item.itemName}");
            Debug.Log($"<color=red>Too heavy!</color> Cannot pick up {item.itemName}.");
            return false;
        }

        // Logic to determine if an item can be picked up
        public bool CanCarry(ItemData item)
        {
            float potentialWeight = GetTotalWeight() + item.weight;
            // EncumbranceLimit was defined in Milestone 7b (usually Constitution * 2)
            return potentialWeight <= stats.EncumbranceLimit;
        }
    }
}