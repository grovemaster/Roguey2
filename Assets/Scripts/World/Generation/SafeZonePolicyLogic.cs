using System.Collections.Generic;
using JRogue.Item;
using UnityEngine;

namespace JRogue.World.Generation
{
    /// <summary>Pure policy resolution for gameplay safe zones (unit-testable).</summary>
    public static class SafeZonePolicyLogic
    {
        public static bool IsUtilityInventoryUse(ItemData item)
        {
            if (item == null)
                return false;

            if (item is DoorKeyItemData)
                return true;

            if (item is FairyStoneItemData)
                return true;

            return item.AllowUseInSafeZone;
        }

        public static FloorCombatPolicy ResolvePolicyAt(
            FloorCombatPolicy floorDefault,
            IReadOnlyList<SafeZoneRegion> regions,
            Vector3Int cell)
        {
            if (regions == null || regions.Count == 0)
                return floorDefault;

            SafeZoneRegion best = default;
            bool hasBest = false;
            int bestArea = int.MaxValue;

            for (int i = 0; i < regions.Count; i++)
            {
                SafeZoneRegion region = regions[i];
                if (!region.Contains(cell))
                    continue;

                int area = region.Area;
                if (!hasBest
                    || area < bestArea
                    || (area == bestArea && region.policy == FloorCombatPolicy.Normal))
                {
                    best = region;
                    bestArea = area;
                    hasBest = true;
                }
            }

            return hasBest ? best.policy : floorDefault;
        }

        public static bool IsSafeZone(FloorCombatPolicy policy) => policy == FloorCombatPolicy.SafeZone;
    }
}
