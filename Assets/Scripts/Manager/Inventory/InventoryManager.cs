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

        /// <summary>Removes up to <paramref name="amount"/> from a carried stack; removes the instance when quantity reaches 0.</summary>
        public bool TryConsumeCarriedQuantity(ItemInstance instance, int amount = 1)
        {
            if (instance == null || amount < 1 || !carriedItems.Contains(instance))
                return false;

            if (instance.Quantity > amount)
            {
                instance.Quantity -= amount;
                return true;
            }

            if (instance.Quantity == amount)
                return TryRemoveCarried(instance);

            return false;
        }

        public bool AddItem(ItemInstance instance)
        {
            if (instance == null || instance.Definition == null)
            {
                Debug.LogWarning("[Inventory] AddItem rejected: null instance or definition.");
                return false;
            }

            if (instance.IsManaStone && instance.Definition is ManaStoneItemData manaStone)
            {
                if (PartyManaStoneLedger.Instance != null)
                    PartyManaStoneLedger.Instance.Add(
                        manaStone.tier,
                        instance.ManaStoneSourceSpeciesId,
                        instance.Quantity);
                else
                    Debug.LogWarning("[Inventory] Mana stone pickup but no PartyManaStoneLedger in scene.");
                return true;
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
            if (instance.Definition is EvocableItemData evocable)
            {
                instance.Quantity = 1;
                if (instance.MaxCharges < 1)
                    EvocableChargeRules.InitializeCharges(instance, evocable);
                else
                    EvocableChargeRules.ClampCharges(instance);
            }

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
            if (instance.IsManaStone || instance.IsCurrency)
                return true;

            float potential = GetTotalWeight() + instance.TotalWeight;
            return potential <= stats.EncumbranceLimit;
        }

#if UNITY_EDITOR
        /// <summary>Sample-scene editor seeding — keeps ScriptableObject refs on prefab instances.</summary>
        public void EditorReplaceBowKitItems(
            ItemData bow,
            ItemData stoneArrow,
            ItemData steelArrow,
            int stoneQty,
            int steelQty)
        {
            for (int i = carriedItems.Count - 1; i >= 0; i--)
            {
                ItemData def = carriedItems[i]?.Definition;
                if (def == null || def.IsBowWeapon || def.IsBowAmmo)
                    carriedItems.RemoveAt(i);
            }

            AddCarriedForEditorSeed(bow, 1);
            AddCarriedForEditorSeed(stoneArrow, stoneQty);
            AddCarriedForEditorSeed(steelArrow, steelQty);
        }

        /// <summary>Editor menu seeding — avoids SerializedObject on prefab instances (SO refs often drop).</summary>
        public void EditorSeedCarriedItem(ItemData definition, int quantity)
        {
            if (definition == null || quantity < 1)
                return;

            for (int i = carriedItems.Count - 1; i >= 0; i--)
            {
                if (carriedItems[i]?.Definition == null)
                    carriedItems.RemoveAt(i);
            }

            if (definition is EvocableItemData evocable)
            {
                EditorAddEvocableInstance(evocable, quantity);
                return;
            }

            for (int i = 0; i < carriedItems.Count; i++)
            {
                if (carriedItems[i]?.Definition == definition)
                {
                    carriedItems[i].Quantity = quantity;
                    carriedItems[i].StorageLocation = ItemStorageLocation.Carried;
                    carriedItems[i].IsAppraised = true;
                    return;
                }
            }

            AddCarriedForEditorSeed(definition, quantity);
        }

        void AddCarriedForEditorSeed(ItemData definition, int quantity)
        {
            if (definition == null || quantity < 1)
                return;

            var inst = new ItemInstance(definition, quantity)
            {
                StorageLocation = ItemStorageLocation.Carried,
                IsAppraised = true,
            };
            carriedItems.Add(inst);
        }

        /// <summary>Always adds a new evocable row (no merge by definition).</summary>
        public void EditorAddEvocableInstance(EvocableItemData definition, int startingCharges)
        {
            if (definition == null)
                return;

            ItemInstance inst = ItemInstance.CreateEvocable(definition, startingCharges);
            inst.StorageLocation = ItemStorageLocation.Carried;
            inst.IsAppraised = true;
            carriedItems.Add(inst);
        }
#endif
    }
}
