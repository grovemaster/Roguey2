using JRogue.Combat;
using JRogue.Actors;
using JRogue.Item;
using JRogue.Manager.Door;
using JRogue.Manager.Equipment;
using JRogue.Manager.Inventory;
using JRogue.Stats;
using JRogue.World.Generation;
using UnityEngine;

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

            if (SafeZonePolicyService.IsSafeZoneForActiveParty()
                && !SafeZonePolicyService.IsUtilityInventoryUse(item))
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

                case ItemCategory.Key:
                    if (row.IsEquipped || row.Owner == null)
                        return false;
                    if (row.Item is not DoorKeyItemData key || string.IsNullOrEmpty(key.targetDoorId))
                        return false;
                    if (!TryFindAdjacentLockedDoor(row.Owner, key.targetDoorId))
                        return false;
                    if (!inCombat)
                        return true;
                    return InventoryPolicy.CanUseCarriedFromAlly(row.Owner, row.Owner, itemEquippedElsewhere: false);

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

        static bool TryFindAdjacentLockedDoor(BaseActor owner, string doorId)
        {
            DoorService doors = DoorService.Instance;
            if (doors == null || owner == null)
                return false;

            Vector3Int[] ortho = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };
            for (int i = 0; i < ortho.Length; i++)
            {
                if (!doors.TryGetAtCell(owner.GridPosition + ortho[i], out DoorInstance door))
                    continue;

                if (door.DoorId == doorId && !door.IsUnlocked)
                    return true;
            }

            return false;
        }
    }
}
