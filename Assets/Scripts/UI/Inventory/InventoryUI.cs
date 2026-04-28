using UnityEngine;
using TMPro;
using JRogue.Item;
using JRogue.Stats;
using JRogue.Manager.Inventory;
using JRogue.Manager.Equipment;

namespace JRogue.UI.Inventory
{
    public class InventoryUI : MonoBehaviour
    {
        [Header("System Links")]
        public InventoryManager playerInventory;
        public GameObject inventoryPanel; // Drag your Panel here

        [Header("UI Elements")]
        public TextMeshProUGUI weightText;
        public Transform itemContainer; // The object with the Vertical Layout Group
        public GameObject itemRowPrefab; // The button template we'll make next

        void Update()
        {
            //if (Input.GetKeyDown(KeyCode.I))
            if (UnityEngine.InputSystem.Keyboard.current.iKey.wasPressedThisFrame)
            {
                inventoryPanel.SetActive(!inventoryPanel.activeSelf);

                if (inventoryPanel.activeSelf)
                {
                    RefreshInventoryDisplay();
                }
            }
        }

        public void RefreshInventoryDisplay()
        {
            Debug.Log("Refreshing inventory UI");
            // 1. Clear current list
            foreach (Transform child in itemContainer)
            {
                Destroy(child.gameObject);
            }

            // 2. Get the EquipmentManager to check for current weapon
            var equipManager = playerInventory.GetComponent<EquipmentManager>();

            // 3. Populate list from InventoryManager
            foreach (ItemData item in playerInventory.items)
            {
                GameObject row = Instantiate(itemRowPrefab, itemContainer);

                // Find the Text component in the prefab to set the name
                TextMeshProUGUI rowText = row.GetComponentInChildren<TextMeshProUGUI>();

                // Add a visual indicator if this item is currently equipped
                string equippedPrefix = (item == equipManager.GetItemFromEquipmentSlot(EquipmentSlot.MainHand)) ? "[E] " : "";
                // string equippedPrefix = (item == equipManager.equippedWeapon) ? "[E] " : "";
                rowText.text = $"{equippedPrefix}{item.itemName} ({item.weight}kg)";

                // 4. Set up the Button Click
                UnityEngine.UI.Button btn = row.GetComponent<UnityEngine.UI.Button>();
                btn.onClick.AddListener(() =>
                {
                    // Trigger your existing OnUnequip/OnEquip logic via the manager
                    equipManager.EquipItem(EquipmentSlot.MainHand, item);
                    // equipManager.EquipWeapon(item);

                    // Refresh the UI so the "[E]" prefix moves to the new item
                    RefreshInventoryDisplay();
                });
            }

            // 3. Update Weight
            CharacterStats stats = playerInventory.GetComponent<CharacterStats>();
            float currentWeight = playerInventory.GetTotalWeight();
            weightText.text = $"Weight: {currentWeight} / {stats.EncumbranceLimit}";
            weightText.color = currentWeight > stats.EncumbranceLimit ? Color.red : Color.white;
        }
    }
}