using JRogue.Item;
using JRogue.Stats;
using JRogue.Stats.Racial;
using UnityEngine;

namespace JRogue.Manager.Equipment
{
    /// <summary>Phase 4 — central gate for anatomy/tag-driven equip rules.</summary>
    public static class EquipmentLegalityEvaluator
    {
        /// <returns>True when <paramref name="actor"/> may equip <paramref name="item"/> into <paramref name="intendedSlot"/>.</returns>
        public static bool CanEquip(GameObject actor, ItemData item, EquipmentSlot intendedSlot, out string reason)
        {
            reason = null;

            if (actor == null || item == null)
            {
                reason = "Missing actor or item.";
                return false;
            }

            if (item.slotType != intendedSlot)
            {
                reason = $"Item uses slot {item.slotType}, not {intendedSlot}.";
                return false;
            }

            var equip = actor.GetComponent<EquipmentManager>();
            ItemData mainHand = equip?.GetItemFromEquipmentSlot(EquipmentSlot.MainHand);
            bool bowWielded = mainHand != null && mainHand.IsBowWeapon;

            if (intendedSlot == EquipmentSlot.OffHand)
            {
                if (bowWielded && !item.IsBowAmmo)
                {
                    reason = $"Cannot equip {item.itemName}: bow requires arrow ammo in off hand.";
                    Debug.Log($"[Bow] {reason}");
                    return false;
                }

                if (item.IsBowAmmo && !bowWielded)
                {
                    reason = "Arrows require a bow in the main hand.";
                    return false;
                }
            }

            if (intendedSlot == EquipmentSlot.MainHand && item.IsBowWeapon && equip != null)
            {
                ItemInstance off = equip.GetEquippedInstance(EquipmentSlot.OffHand);
                if (off?.Definition != null && !off.Definition.IsBowAmmo)
                {
                    reason = "Unequip the off-hand item before wielding a bow.";
                    return false;
                }
            }

            if (intendedSlot == EquipmentSlot.MainHand && bowWielded == false && item.IsBowAmmo)
            {
                reason = "Arrows equip only to the off hand.";
                return false;
            }

            var stats = actor.GetComponent<CharacterStats>();
            if (stats == null)
            {
                reason = "Actor has no CharacterStats.";
                return false;
            }

            BodyCapabilityFlags effective = stats.GetEffectiveBodyCapabilities();
            BodyCapabilityFlags required = item.equipRequiresAllFlags;

            if (required != BodyCapabilityFlags.None && (effective & required) != required)
            {
                reason = $"Missing required body capability (need {required}, effective {effective}).";
                return false;
            }

            BodyCapabilityFlags excluded = item.equipExcludesActorFlags;
            if (excluded != BodyCapabilityFlags.None)
            {
                BodyCapabilityFlags bypass = stats.GetBodyExclusionBypassMask();
                BodyCapabilityFlags conflict = (effective & excluded) & ~bypass;
                if (conflict != BodyCapabilityFlags.None)
                {
                    reason = $"Body conflicts with this item ({conflict}).";
                    return false;
                }
            }

            return true;
        }
    }
}
