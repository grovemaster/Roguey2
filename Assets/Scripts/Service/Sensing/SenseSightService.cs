using JRogue.Actors;
using JRogue.Manager.Map;
using JRogue.Manager.Visibility.Algorithm;
using UnityEngine;

namespace Roguey2.Sensing
{
    public enum ConeVisionZone
    {
        None,
        Primary,
        Peripheral
    }

    /// <summary>
    /// Stateless helper for enemy sight checks (zone classification + LOS).
    /// </summary>
    public static class ConeSightUtility
    {
        public static ConeVisionZone GetVisionZone(
            BaseActor observer,
            Vector3Int targetPos,
            int maxRange,
            float primaryConeAngle,
            float peripheralRangeMultiplier)
        {
            Vector2 toTarget = new Vector2(targetPos.x - observer.GridPosition.x, targetPos.y - observer.GridPosition.y);
            float distance = toTarget.magnitude;
            if (distance <= 0.001f) return ConeVisionZone.Primary;
            if (distance > maxRange) return ConeVisionZone.None;

            float halfPrimaryAngle = Mathf.Clamp(primaryConeAngle, 0f, 180f) * 0.5f;
            float angleFromFacing = Vector2.Angle(observer.GetFacingVector(), toTarget.normalized);
            bool inPrimary = angleFromFacing <= halfPrimaryAngle;

            float peripheralMultiplier = Mathf.Clamp(peripheralRangeMultiplier, 0.1f, 1f);
            float zoneMaxRange = inPrimary ? maxRange : maxRange * peripheralMultiplier;
            if (distance > zoneMaxRange) return ConeVisionZone.None;

            return inPrimary ? ConeVisionZone.Primary : ConeVisionZone.Peripheral;
        }

        public static bool TrySenseTarget(
            BaseActor observer,
            Vector3Int targetPos,
            MapManager mapManager,
            int maxRange,
            float primaryConeAngle,
            float peripheralRangeMultiplier,
            out ConeVisionZone zone)
        {
            zone = GetVisionZone(observer, targetPos, maxRange, primaryConeAngle, peripheralRangeMultiplier);
            if (zone == ConeVisionZone.None) return false;
            if (mapManager == null) return false;

            int losRange = zone == ConeVisionZone.Primary
                ? maxRange
                : Mathf.Max(1, Mathf.RoundToInt(maxRange * Mathf.Clamp(peripheralRangeMultiplier, 0.1f, 1f)));

            Vector3Int origin = new Vector3Int(observer.GridPosition.x, observer.GridPosition.y, 0);
            Vector3Int target = new Vector3Int(targetPos.x, targetPos.y, 0);

            var visibleTiles = ShadowCaster.GetVisibleTiles(
                origin,
                losRange,
                pos => !mapManager.IsWalkable(pos));

            return visibleTiles.Contains(target);
        }
    }
}
