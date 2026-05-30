using JRogue.Combat;
using JRogue.Item;
using JRogue.Manager.Equipment;
using JRogue.Manager.Inventory;
using JRogue.Stats;

namespace JRogue.UI.Inventory
{
    /// <summary>Centralized &quot;can this appear as usable now?&quot; for filtered views.</summary>
    public static class InventoryUsability
    {
        public static bool AppearsUsableNow(InventoryViewModel.Row row, bool inCombat)
        {
            ItemData item = row.Item;
            if (item == null)
                return false;

            if (row.Instance != null && row.Instance.StorageLocation == ItemStorageLocation.OnGround)
                return false;

            if (item.IsBowAmmo)
            {
                if (row.IsEquipped)
                    return false;
                if (row.Owner == null || !BowRangedCombatService.HasBowEquipped(row.Owner))
                    return false;
                if (!inCombat)
                    return true;
                return InventoryPolicy.CanUseCarriedFromAlly(row.Owner, row.Owner, itemEquippedElsewhere: false);
            }

            bool hasActiveAbility = item.activeAbilities != null && item.activeAbilities.Count > 0;

            switch (item.category)
            {
                case ItemCategory.Potion:
                    if (row.IsEquipped)
                        return false;
                    if (row.Owner != null &&
                        row.Owner.TryGetComponent(out CharacterStats stats) &&
                        stats.race == Race.Undead)
                        return false;
                    if (HealingPotionRules.IsHealingPotionItem(item))
                    {
                        if (row.Owner == null)
                            return false;
                        if (!inCombat)
                            return true;
                        return HealingPotionRules.IsExemptFromPainStun(row.Owner.gameObject);
                    }

                    goto case ItemCategory.Scroll;
                case ItemCategory.Scroll:
                    if (row.IsEquipped)
                        return false;
                    if (!inCombat)
                        return row.Owner != null;
                    return row.Owner != null &&
                           InventoryPolicy.CanUseCarriedFromAlly(row.Owner, row.Owner, itemEquippedElsewhere: false);

                case ItemCategory.Evocable:
                    if (row.IsEquipped)
                        return false;
                    if (row.Instance == null || !EvocableChargeRules.HasChargeToInvoke(row.Instance))
                        return false;
                    if (EvocableChargeRules.GetInvokeAbility(item) == null)
                        return false;
                    if (!inCombat)
                        return row.Owner != null;
                    return row.Owner != null &&
                           InventoryPolicy.CanUseCarriedFromAlly(row.Owner, row.Owner, itemEquippedElsewhere: false);

                default:
                    if (!hasActiveAbility)
                        return false;
                    return !row.IsEquipped && row.Owner != null;
            }
        }
    }
}
