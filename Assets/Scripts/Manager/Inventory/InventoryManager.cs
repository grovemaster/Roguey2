using System.Collections.Generic;
using System.Linq;
using JRogue.Item;
using JRogue.Manager.Equipment;
using JRogue.Manager.Party;
using JRogue.Stats;
using UnityEngine;

namespace JRogue.Manager.Inventory
{
    public class InventoryManager : MonoBehaviour
    {
        [SerializeField] List<ItemInstance> carriedItems = new List<ItemInstance>();

        [SerializeField] InventoryAutomationProfile automationProfile;

        CharacterStats stats;

        void Awake() => stats = GetComponent<CharacterStats>();

        public IReadOnlyList<ItemInstance> CarriedItems => carriedItems;

        public float GetCarriedWeight() => carriedItems.Sum(i => i.TotalWeight);

        public float GetTotalWeight()
        {
            float w = GetCarriedWeight();
            EquipmentManager eq = GetComponent<EquipmentManager>();
            if (eq != null)
                w += eq.GetEquippedWeight();
            return w;
        }

        public bool TryRemoveCarriedAt(int index)
        {
            if (index < 0 || index >= carriedItems.Count)
                return false;
            carriedItems.RemoveAt(index);
            return true;
        }

        public bool TryRemoveCarried(ItemInstance instance)
        {
            return instance != null && carriedItems.Remove(instance);
        }

        public bool AddItem(ItemInstance instance)
        {
            if (instance == null || instance.Definition == null)
            {
                Debug.LogWarning("[Inventory] AddItem rejected: null instance or definition.");
                return false;
            }

            if (instance.IsCurrency)
            {
                if (PartyCurrencyLedger.Instance != null)
                    PartyCurrencyLedger.Instance.Add(instance.Definition, instance.Quantity);
                else
                    Debug.LogWarning("[Inventory] Currency pickup but no PartyCurrencyLedger in scene.");
                return true;
            }

            if (!CanCarry(instance))
            {
                Debug.LogWarning($"Too heavy! Cannot carry {instance.Definition.itemName}");
                return false;
            }

            instance.StorageLocation = ItemStorageLocation.Carried;
            carriedItems.Add(instance);

            if (automationProfile != null)
            {
                if (automationProfile.ShouldAutoJunk(instance.Definition.category))
                    instance.UserMarks |= ItemUserMark.Junk;

                if (automationProfile.sortCarriedAfterEveryPickup)
                    InventoryCarriedSorter.SortInPlace(carriedItems);
            }

            Debug.Log(
                $"Inventory: Added {instance.Definition.itemName} [{instance.Id}]. Weight: {GetTotalWeight()}/{stats.EncumbranceLimit}");
            return true;
        }

        public bool CanCarry(ItemInstance instance)
        {
            if (instance == null || instance.Definition == null)
                return false;
            if (instance.IsCurrency)
                return true;

            float potential = GetTotalWeight() + instance.TotalWeight;
            return potential <= stats.EncumbranceLimit;
        }
    }
}
