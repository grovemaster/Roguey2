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
            JRogue.Quest.QuestService.Instance?.NotifyInventoryChanged();
            return true;
        }

        public bool TryRemoveCarried(ItemInstance instance)
        {
            bool removed = instance != null && carriedItems.Remove(instance);
            if (removed)
                JRogue.Quest.QuestService.Instance?.NotifyInventoryChanged();
            return removed;
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

        /// <summary>Whether carried stacks with the same <see cref="ItemData"/> may combine (e.g. on unequip).</summary>
        public static bool CanMergeCarriedStacks(ItemData definition)
        {
            if (definition == null)
                return false;

            if (definition.category == ItemCategory.Evocable)
                return false;

            return definition.category != ItemCategory.Currency && definition is not ManaStoneItemData;
        }

        /// <summary>
        /// Removes one unit from a carried stack for equipping. Bow ammo equips the whole stack instead
        /// (caller should use <see cref="TryRemoveCarried"/>).
        /// </summary>
        public bool TrySplitCarriedForEquip(ItemInstance stack, out ItemInstance equipInstance)
        {
            equipInstance = null;
            if (stack == null || stack.Definition == null || !carriedItems.Contains(stack))
                return false;

            if (stack.Definition.IsBowAmmo)
                return false;

            if (stack.Quantity <= 1)
            {
                if (!TryRemoveCarried(stack))
                    return false;

                equipInstance = stack;
                equipInstance.Quantity = 1;
                return true;
            }

            stack.Quantity -= 1;
            equipInstance = CreateSplitInstance(stack, 1);
            return true;
        }

        /// <summary>
        /// Returns an equipped or loose item to the bag, merging into an existing stack when allowed.
        /// </summary>
        public bool TryStowCarriedItem(ItemInstance instance)
        {
            if (instance == null || instance.Definition == null)
            {
                Debug.LogWarning("[Inventory] TryStowCarriedItem rejected: null instance or definition.");
                return false;
            }

            if (instance.IsManaStone || instance.IsCurrency)
                return AddItem(instance);

            if (!CanCarry(instance))
            {
                Debug.LogWarning($"Too heavy! Cannot stow {instance.Definition.itemName}");
                return false;
            }

            instance.StorageLocation = ItemStorageLocation.Carried;

            if (CanMergeCarriedStacks(instance.Definition))
            {
                for (int i = 0; i < carriedItems.Count; i++)
                {
                    ItemInstance existing = carriedItems[i];
                    if (existing?.Definition != instance.Definition)
                        continue;

                    existing.Quantity += instance.Quantity;
                    Debug.Log(
                        $"Inventory: Stowed {instance.Quantity} × {instance.Definition.itemName} into existing stack [{existing.Id}]. Weight: {GetTotalWeight()}/{stats.EncumbranceLimit}");
                    return true;
                }
            }

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
                $"Inventory: Stowed {instance.Definition.itemName} [{instance.Id}]. Weight: {GetTotalWeight()}/{stats.EncumbranceLimit}");
            return true;
        }

        static ItemInstance CreateSplitInstance(ItemInstance source, int quantity)
        {
            var split = new ItemInstance(source.Definition, quantity)
            {
                IsAppraised = source.IsAppraised,
                UserMarks = source.UserMarks,
                UserInscription = source.UserInscription,
                StorageLocation = ItemStorageLocation.Equipped,
            };
            return split;
        }

        public bool AddItem(ItemInstance instance)
        {
            if (instance == null || instance.Definition == null)
            {
                Debug.LogWarning("[Inventory] AddItem rejected: null instance or definition.");
                return false;
            }

            if (instance.Definition != null && instance.Definition.category == ItemCategory.Essence)
            {
                Debug.LogWarning("[Inventory] Essences cannot be stored in inventory.");
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
            JRogue.Quest.QuestService.Instance?.NotifyInventoryChanged();
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
