using System.Collections.Generic;
using System.Linq;
using JRogue.Ability;
using JRogue.Combat;
using JRogue.Item;
using JRogue.Manager.Inventory;
using JRogue.Racial;
using JRogue.Stats;
using UnityEngine;

namespace JRogue.Manager.Equipment
{
    public class EquipmentManager : MonoBehaviour
    {
        readonly Dictionary<EquipmentSlot, ItemInstance> _equipment = new Dictionary<EquipmentSlot, ItemInstance>();

        CharacterStats stats;

        void Awake() => stats = GetComponent<CharacterStats>();

        public float GetEquippedWeight() =>
            _equipment.Values.Where(v => v != null).Sum(i => i.TotalWeight);

        public IReadOnlyDictionary<EquipmentSlot, ItemInstance> EquippedSnapshot => _equipment;

        public void EquipItem(EquipmentSlot slot, ItemInstance newItem)
        {
            InventoryManager inv = GetComponent<InventoryManager>();

            if (newItem?.Definition != null
                && newItem.Definition.IsBowWeapon
                && slot == EquipmentSlot.MainHand)
            {
                TryClearIllegalOffHandForBow(inv);
            }

            if (newItem?.Definition != null
                && !EquipmentLegalityEvaluator.CanEquip(gameObject, newItem.Definition, slot, out string illegalReason))
            {
                Debug.LogWarning($"[Equip] Cannot equip {newItem.Definition.itemName}: {illegalReason}");
                return;
            }

            if (newItem != null && inv != null)
            {
                bool inBag = false;
                foreach (ItemInstance c in inv.CarriedItems)
                {
                    if (c != null && c.Id == newItem.Id)
                    {
                        inBag = true;
                        break;
                    }
                }

                if (!inBag)
                {
                    Debug.LogWarning($"[Equip] {newItem.Definition?.itemName} ({newItem.Id}) is not in {name}'s bag.");
                    return;
                }

                inv.TryRemoveCarried(newItem);
                newItem.StorageLocation = ItemStorageLocation.Equipped;
            }

            if (_equipment.TryGetValue(slot, out ItemInstance oldItem) && oldItem != null)
            {
                foreach (var mod in oldItem.Definition.statModifiers)
                {
                    var s = stats.GetStatByType(mod.targetStat);
                    s?.RemoveModifiersFromSource(oldItem.Definition);
                }

                foreach (var passive in oldItem.Definition.passiveEffects)
                    passive.OnRemove(gameObject);

                _equipment.Remove(slot);

                if (inv != null && oldItem.Definition != null)
                {
                    oldItem.StorageLocation = ItemStorageLocation.Carried;
                    inv.AddItem(oldItem);
                }
            }

            if (newItem != null && newItem.Definition != null)
            {
                _equipment[slot] = newItem;
                newItem.StorageLocation = ItemStorageLocation.Equipped;

                foreach (var mod in newItem.Definition.statModifiers)
                {
                    var s = stats.GetStatByType(mod.targetStat);
                    s?.AddModifier(mod.modifierAmount, newItem.Definition);
                }

                foreach (var passive in newItem.Definition.passiveEffects)
                    passive.OnApply(gameObject);

                Debug.Log($"Equipped {newItem.Definition.itemName} to {slot} (instance {newItem.Id}).");

                if (slot == EquipmentSlot.OffHand && newItem.Definition.IsBowAmmo)
                    BowRangedCombatService.LogDefaultAmmo(newItem.Definition, newItem.Quantity);
            }
        }

        void TryClearIllegalOffHandForBow(InventoryManager inv)
        {
            if (!_equipment.TryGetValue(EquipmentSlot.OffHand, out ItemInstance off) || off?.Definition == null)
                return;

            if (off.Definition.IsBowAmmo)
                return;

            if (!TryUnequipToBag(EquipmentSlot.OffHand))
            {
                Debug.LogWarning(
                    $"[Bow] Cannot wield bow: off hand occupied by {off.Definition.itemName} and cannot stow it.");
            }
        }

        /// <summary>Consumes ammo from the equipped off-hand stack. Promotes next carried stack at 0.</summary>
        public bool TryConsumeEquippedAmmo(int amount, out ItemData consumedDefinition)
        {
            consumedDefinition = null;
            if (amount < 1)
                return false;

            if (!_equipment.TryGetValue(EquipmentSlot.OffHand, out ItemInstance stack)
                || stack?.Definition == null
                || !stack.Definition.IsBowAmmo)
            {
                return false;
            }

            consumedDefinition = stack.Definition;

            if (stack.Quantity > amount)
            {
                stack.Quantity -= amount;
                return true;
            }

            if (stack.Quantity < amount)
                return false;

            _equipment.Remove(EquipmentSlot.OffHand);
            TryPromoteNextArrowStack();
            return true;
        }

        /// <summary>Equips the next carried bow-ammo stack into off-hand (inventory sort order).</summary>
        public void TryPromoteNextArrowStack()
        {
            InventoryManager inv = GetComponent<InventoryManager>();
            if (inv == null)
            {
                BowRangedCombatService.LogNoArrowsRemaining();
                return;
            }

            var candidates = new List<ItemInstance>();
            foreach (ItemInstance inst in inv.CarriedItems)
            {
                if (inst?.Definition != null && inst.Definition.IsBowAmmo && inst.Quantity > 0)
                    candidates.Add(inst);
            }

            if (candidates.Count == 0)
            {
                BowRangedCombatService.LogNoArrowsRemaining();
                return;
            }

            InventoryCarriedSorter.SortInPlace(candidates);
            EquipItem(EquipmentSlot.OffHand, candidates[0]);
            BowRangedCombatService.LogPromotedAmmo(candidates[0].Definition, candidates[0].Quantity);
        }

        /// <summary>Ensures off-hand has ammo when possible (for Aim key).</summary>
        public void TryEnsureDefaultAmmoEquipped()
        {
            if (GetEquippedInstance(EquipmentSlot.OffHand) is { Quantity: > 0 } existing
                && existing.Definition?.IsBowAmmo == true)
            {
                return;
            }

            TryPromoteNextArrowStack();
        }

        /// <summary>Moves equipped item back to this actor&apos;s carried list if weight allows.</summary>
        public bool TryUnequipToBag(EquipmentSlot slot)
        {
            InventoryManager inv = GetComponent<InventoryManager>();
            if (inv == null)
                return false;

            if (!_equipment.TryGetValue(slot, out ItemInstance inst) || inst?.Definition == null)
                return false;

            if (slot == EquipmentSlot.MainHand && inst.Definition.IsBowWeapon)
            {
                ItemInstance off = GetEquippedInstance(EquipmentSlot.OffHand);
                if (off?.Definition != null && off.Definition.IsBowAmmo)
                    TryUnequipToBag(EquipmentSlot.OffHand);
            }

            if (!inv.CanCarry(inst))
            {
                Debug.LogWarning($"[Unequip] Too encumbered to stow {inst.Definition.itemName} ({slot}).");
                return false;
            }

            foreach (var mod in inst.Definition.statModifiers)
            {
                var s = stats.GetStatByType(mod.targetStat);
                s?.RemoveModifiersFromSource(inst.Definition);
            }

            foreach (var passive in inst.Definition.passiveEffects)
                passive.OnRemove(gameObject);

            _equipment.Remove(slot);
            inst.StorageLocation = ItemStorageLocation.Carried;
            inv.AddItem(inst);
            Debug.Log($"[Unequip] Moved {inst.Definition.itemName} from {slot} to bag ({inst.Id}).");
            return true;
        }

        public int GetTotalAttack(int baseStats)
        {
            int total = baseStats;

            if (_equipment.TryGetValue(EquipmentSlot.MainHand, out ItemInstance weapon)
                && weapon?.Definition != null)
            {
                int weaponDamage = weapon.Definition.damageModules.Sum(m => m.value);
                total += weaponDamage;
            }

            var spiritContracts = GetComponent<ElementalSpiritContractsRuntime>();
            if (spiritContracts != null)
                total += spiritContracts.WeaponFireImbueBonus;

            return total;
        }

        public bool TryExecuteItemAbility(int slotIndex, int abilityIndex) =>
            TryExecuteItemInternal(slotIndex, abilityIndex, false, default);

        public bool TryExecuteItemAbility(int slotIndex, int abilityIndex, Vector3Int targetTile) =>
            TryExecuteItemInternal(slotIndex, abilityIndex, true, targetTile);

        bool TryExecuteItemInternal(int slotIndex, int abilityIndex, bool useTarget, Vector3Int targetTile)
        {
            EquipmentSlot targetSlot = MapIndexToSlot(slotIndex);
            if (!_equipment.TryGetValue(targetSlot, out ItemInstance item) || item?.Definition == null)
                return false;
            if (item.Definition.activeAbilities == null || abilityIndex >= item.Definition.activeAbilities.Count)
                return false;

            AbilityAction ability = item.Definition.activeAbilities[abilityIndex];
            if (!ability.CanExecute(gameObject))
                return false;

            return useTarget
                ? ability.Execute(gameObject, targetTile)
                : ability.Execute(gameObject);
        }

        public ItemData GetItemFromEquipmentSlot(EquipmentSlot equipmentSlot)
        {
            return _equipment.TryGetValue(equipmentSlot, out ItemInstance equippedItem)
                ? equippedItem?.Definition
                : null;
        }

        public ItemInstance GetEquippedInstance(EquipmentSlot equipmentSlot) =>
            _equipment.TryGetValue(equipmentSlot, out ItemInstance i) ? i : null;

        public bool TryGetEquippedSlot(ItemInstance item, out EquipmentSlot slot)
        {
            foreach (KeyValuePair<EquipmentSlot, ItemInstance> kv in _equipment)
            {
                if (kv.Value != null && item != null && kv.Value.Id == item.Id)
                {
                    slot = kv.Key;
                    return true;
                }
            }

            slot = default;
            return false;
        }

        public AbilityAction GetItemAbility(int slotIndex, int abilityIndex)
        {
            EquipmentSlot targetSlot = MapIndexToSlot(slotIndex);

            if (_equipment.TryGetValue(targetSlot, out ItemInstance item)
                && item?.Definition?.activeAbilities != null
                && abilityIndex < item.Definition.activeAbilities.Count)
            {
                return item.Definition.activeAbilities[abilityIndex];
            }

            return null;
        }

        static EquipmentSlot MapIndexToSlot(int index)
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
