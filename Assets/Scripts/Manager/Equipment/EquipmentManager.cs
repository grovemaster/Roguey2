using System.Collections.Generic;
using System.Linq;
using JRogue.Ability;
using JRogue.Item;
using JRogue.Stats;
using UnityEngine;

namespace JRogue.Manager.Equipment
{
    public class EquipmentManager : MonoBehaviour
    {
        // This stores everything: Weapon, Armor, Rings, etc.
        [SerializeField]
        private Dictionary<EquipmentSlot, ItemData> currentEquipment = new Dictionary<EquipmentSlot, ItemData>();

        private CharacterStats stats;

        void Awake() // Better to cache stats in Awake
        {
            stats = GetComponent<CharacterStats>();
        }

        public void EquipItem(EquipmentSlot slot, ItemData newItem)
        {
            // 1. Unequip the old item and CLEAN UP all its effects
            if (currentEquipment.TryGetValue(slot, out ItemData oldItem) && oldItem != null)
            {
                // Remove Stat Modifiers (Source-based removal)
                foreach (var mod in oldItem.statModifiers)
                {
                    var stat = stats.GetStatByType(mod.targetStat);
                    stat?.RemoveModifiersFromSource(oldItem);
                }

                // Remove all Passive Logic Hooks
                foreach (var passive in oldItem.passiveEffects)
                {
                    passive.OnRemove(gameObject);
                }

                currentEquipment.Remove(slot);
            }

            // 2. Equip the new item and INITIALIZE all effects
            if (newItem != null)
            {
                currentEquipment[slot] = newItem;

                // Apply Stat Modifiers
                foreach (var mod in newItem.statModifiers)
                {
                    var stat = stats.GetStatByType(mod.targetStat);
                    stat?.AddModifier(mod.modifierAmount, newItem); // 'newItem' is the Source
                }

                // Apply all Passive Logic Hooks
                foreach (var passive in newItem.passiveEffects)
                {
                    passive.OnApply(gameObject);
                }

                Debug.Log($"Equipped {newItem.itemName} to {slot}. Stats recalculated.");
            }
        }

        // Keep your specialized weapon logic, but pull from the dictionary
        public int GetTotalAttack(int baseStats)
        {
            int total = baseStats;

            if (currentEquipment.TryGetValue(EquipmentSlot.MainHand, out ItemData weapon))
            {
                int weaponDamage = weapon.damageModules.Sum(module => module.value);
                total += weaponDamage;
                Debug.Log("The weapon damage module size is " + weapon.damageModules.Count);
                Debug.Log("The weapon damage is " + weaponDamage);

            }

            return total;
        }

        // Logic for the InputHandler to find an active ability from gear
        public bool TryExecuteItemAbility(int slotIndex, int abilityIndex)
        {
            // 1. Map the slot index (Ctrl+1, Ctrl+2) to an EquipmentSlot
            EquipmentSlot targetSlot = MapIndexToSlot(slotIndex);

            // 2. See if we have an item in that slot
            if (currentEquipment.TryGetValue(targetSlot, out ItemData item))
            {
                // 3. Check if the item has the requested ability index (e.g., Ctrl+Shift+1)
                if (item.activeAbilities != null && abilityIndex < item.activeAbilities.Count)
                {
                    AbilityAction ability = item.activeAbilities[abilityIndex];

                    if (ability.CanExecute(gameObject))
                    {
                        return ability.Execute(gameObject);
                    }
                }
            }
            return false;
        }

        public ItemData GetItemFromEquipmentSlot(EquipmentSlot equipmentSlot)
        {
            if (currentEquipment.TryGetValue(equipmentSlot, out ItemData equippedItem))
            {
                return equippedItem;
            }

            return null;
        }

        // Inside EquipmentManager.cs
        public AbilityAction GetItemAbility(int slotIndex, int abilityIndex)
        {
            EquipmentSlot targetSlot = MapIndexToSlot(slotIndex);

            if (currentEquipment.TryGetValue(targetSlot, out ItemData item))
            {
                if (item.activeAbilities != null && abilityIndex < item.activeAbilities.Count)
                {
                    return item.activeAbilities[abilityIndex];
                }
            }
            return null;
        }

        private EquipmentSlot MapIndexToSlot(int index)
        {
            return index switch
            {
                0 => EquipmentSlot.MainHand,
                1 => EquipmentSlot.OffHand,
                2 => EquipmentSlot.Torso,
                3 => EquipmentSlot.Head,
                4 => EquipmentSlot.Accessory_MainHand,
                5 => EquipmentSlot.Accessory_OffHand,
                _ => EquipmentSlot.Accessory_Head
            };
        }
    }
}