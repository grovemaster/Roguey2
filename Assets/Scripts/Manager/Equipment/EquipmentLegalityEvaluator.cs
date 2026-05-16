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
