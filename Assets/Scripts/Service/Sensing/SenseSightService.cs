using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Core.Actor;
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
    /// Multi-tile observers use every footprint cell as a LOS origin (union) and the
    /// nearest occupied cell to the target for cone/range classification.
    /// </summary>
    public static class ConeSightUtility
    {
        static readonly List<Vector3Int> OriginScratch = new List<Vector3Int>(16);
        static readonly HashSet<Vector3Int> CandidateScratch = new HashSet<Vector3Int>();

        public static ConeVisionZone GetVisionZone(
            BaseActor observer,
            Vector3Int targetPos,
            int maxRange,
            float primaryConeAngle,
            float peripheralRangeMultiplier)
        {
            GetSightOriginCells(observer, OriginScratch);
            if (OriginScratch.Count == 0)
                return ConeVisionZone.None;

            Vector3Int nearest = GetNearestOriginCell(OriginScratch, targetPos);
            return GetVisionZoneFromOrigin(
                observer,
                nearest,
                targetPos,
                maxRange,
                primaryConeAngle,
                peripheralRangeMultiplier);
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
            if (zone == ConeVisionZone.None || mapManager == null)
                return false;

            GetSightOriginCells(observer, OriginScratch);
            if (OriginScratch.Count == 0)
                return false;

            int losRange = LosRangeForZone(zone, maxRange, peripheralRangeMultiplier);
            ShadowCaster.IsOpaque isOpaque = pos => !mapManager.IsWalkable(pos);
            Vector3Int target = new Vector3Int(targetPos.x, targetPos.y, 0);

            for (int i = 0; i < OriginScratch.Count; i++)
            {
                if (ShadowCaster.IsVisible(OriginScratch[i], target, losRange, isOpaque))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Appends all grid tiles the observer can see (cone + multi-origin LOS) into <paramref name="output"/>.
        /// Does not clear <paramref name="output"/> — callers union multiple observers by reusing one set.
        /// </summary>
        public static void CollectVisibleTiles(
            BaseActor observer,
            MapManager mapManager,
            int maxRange,
            float primaryConeAngle,
            float peripheralRangeMultiplier,
            HashSet<Vector3Int> output)
        {
            if (observer == null || mapManager == null || output == null)
                return;

            GetSightOriginCells(observer, OriginScratch);
            if (OriginScratch.Count == 0)
                return;

            ShadowCaster.IsOpaque isOpaque = pos => !mapManager.IsWalkable(pos);
            CandidateScratch.Clear();

            for (int i = 0; i < OriginScratch.Count; i++)
            {
                List<Vector3Int> fromOrigin = ShadowCaster.GetVisibleTiles(OriginScratch[i], maxRange, isOpaque);
                for (int j = 0; j < fromOrigin.Count; j++)
                    CandidateScratch.Add(fromOrigin[j]);
            }

            foreach (Vector3Int tile in CandidateScratch)
            {
                Vector3Int nearest = GetNearestOriginCell(OriginScratch, tile);
                ConeVisionZone zone = GetVisionZoneFromOrigin(
                    observer,
                    nearest,
                    tile,
                    maxRange,
                    primaryConeAngle,
                    peripheralRangeMultiplier);
                if (zone == ConeVisionZone.None)
                    continue;

                int losRange = LosRangeForZone(zone, maxRange, peripheralRangeMultiplier);
                for (int o = 0; o < OriginScratch.Count; o++)
                {
                    if (!ShadowCaster.IsVisible(OriginScratch[o], tile, losRange, isOpaque))
                        continue;

                    output.Add(tile);
                    break;
                }
            }
        }

        public static void GetSightOriginCells(BaseActor observer, List<Vector3Int> buffer)
        {
            buffer.Clear();
            if (observer == null)
                return;

            if (observer is IGridFootprint footprint
                && !GridFootprintUtility.IsSingleCell(footprint))
            {
                GridFootprintUtility.GetOccupiedCells(footprint, buffer);
                return;
            }

            buffer.Add(observer.GridPosition);
        }

        public static Vector3Int GetNearestOriginCell(IReadOnlyList<Vector3Int> origins, Vector3Int target)
        {
            Vector3Int best = origins[0];
            int bestDist = ManhattanDistance(best, target);
            for (int i = 1; i < origins.Count; i++)
            {
                int d = ManhattanDistance(origins[i], target);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = origins[i];
                }
            }

            return best;
        }

        public static ConeVisionZone GetVisionZoneFromOrigin(
            BaseActor observer,
            Vector3Int originCell,
            Vector3Int targetPos,
            int maxRange,
            float primaryConeAngle,
            float peripheralRangeMultiplier)
        {
            Vector2 toTarget = new Vector2(targetPos.x - originCell.x, targetPos.y - originCell.y);
            float distance = toTarget.magnitude;
            if (distance <= 0.001f)
                return ConeVisionZone.Primary;
            if (distance > maxRange)
                return ConeVisionZone.None;

            float halfPrimaryAngle = Mathf.Clamp(primaryConeAngle, 0f, 180f) * 0.5f;
            float angleFromFacing = Vector2.Angle(observer.GetFacingVector(), toTarget.normalized);
            bool inPrimary = angleFromFacing <= halfPrimaryAngle;

            float peripheralMultiplier = Mathf.Clamp(peripheralRangeMultiplier, 0.1f, 1f);
            float zoneMaxRange = inPrimary ? maxRange : maxRange * peripheralMultiplier;
            if (distance > zoneMaxRange)
                return ConeVisionZone.None;

            return inPrimary ? ConeVisionZone.Primary : ConeVisionZone.Peripheral;
        }

        static int LosRangeForZone(ConeVisionZone zone, int maxRange, float peripheralRangeMultiplier)
        {
            if (zone == ConeVisionZone.Primary)
                return maxRange;

            return Mathf.Max(1, Mathf.RoundToInt(maxRange * Mathf.Clamp(peripheralRangeMultiplier, 0.1f, 1f)));
        }

        static int ManhattanDistance(Vector3Int a, Vector3Int b) =>
            Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}
