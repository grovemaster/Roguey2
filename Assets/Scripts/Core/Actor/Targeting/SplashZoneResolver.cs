using System.Collections.Generic;
using JRogue.Core.Actor;
using UnityEngine;

namespace JRogue.Core.Targeting
{
    public static class SplashZoneResolver
    {
        static readonly List<Vector3Int> CellScratch = new List<Vector3Int>(64);

        public static SplashZoneDefinition CreateLegacyDisk(int radius)
        {
            var def = ScriptableObject.CreateInstance<SplashZoneDefinition>();
            def.shapeKind = SplashZoneShapeKind.DiskChebyshev;
            def.radius = radius;
            def.includePrimaryInEffect = true;
            def.distanceMetric = SplashZoneDistanceMetric.Chebyshev;
            return def;
        }

        /// <summary>Red preview tiles (excludes primary).</summary>
        public static IReadOnlyList<Vector3Int> GetSplashPreviewCells(
            SplashZoneDefinition zone,
            SplashZoneContext context)
        {
            CellScratch.Clear();
            CollectEffectCells(zone, context, CellScratch);

            for (int i = CellScratch.Count - 1; i >= 0; i--)
            {
                if (CellScratch[i] == context.PrimaryTile)
                    CellScratch.RemoveAt(i);
            }

            return CellScratch;
        }

        /// <summary>All cells that can receive effect damage at confirm.</summary>
        public static IReadOnlyList<Vector3Int> GetEffectCells(
            SplashZoneDefinition zone,
            SplashZoneContext context)
        {
            CellScratch.Clear();
            CollectEffectCells(zone, context, CellScratch);
            return CellScratch;
        }

        static void CollectEffectCells(
            SplashZoneDefinition zone,
            SplashZoneContext context,
            List<Vector3Int> buffer)
        {
            if (zone == null || zone.shapeKind == SplashZoneShapeKind.None)
            {
                buffer.Add(context.PrimaryTile);
                return;
            }

            switch (zone.shapeKind)
            {
                case SplashZoneShapeKind.DiskChebyshev:
                    CollectDisk(context.PrimaryTile, zone.radius, zone.distanceMetric, buffer);
                    break;
                case SplashZoneShapeKind.DiskManhattan:
                    CollectDisk(context.PrimaryTile, zone.radius, SplashZoneDistanceMetric.Manhattan, buffer);
                    break;
                case SplashZoneShapeKind.LineFromCaster:
                    CollectLineFromCaster(context, zone.maxLength, buffer);
                    break;
                default:
                    buffer.Add(context.PrimaryTile);
                    break;
            }

            if (!zone.includePrimaryInEffect && buffer.Count > 0)
            {
                for (int i = buffer.Count - 1; i >= 0; i--)
                {
                    if (buffer[i] == context.PrimaryTile)
                        buffer.RemoveAt(i);
                }
            }
            else if (zone.includePrimaryInEffect && !ContainsCell(buffer, context.PrimaryTile))
            {
                buffer.Add(context.PrimaryTile);
            }
        }

        static void CollectDisk(
            Vector3Int center,
            int radius,
            SplashZoneDistanceMetric metric,
            List<Vector3Int> buffer)
        {
            if (radius <= 0)
            {
                buffer.Add(center);
                return;
            }

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    if (dx == 0 && dy == 0)
                    {
                        buffer.Add(center);
                        continue;
                    }

                    int dist = metric == SplashZoneDistanceMetric.Manhattan
                        ? Mathf.Abs(dx) + Mathf.Abs(dy)
                        : Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));

                    if (dist <= radius)
                        buffer.Add(new Vector3Int(center.x + dx, center.y + dy, center.z));
                }
            }
        }

        static void CollectLineFromCaster(SplashZoneContext context, int maxLength, List<Vector3Int> buffer)
        {
            Vector3Int from = context.CasterCell;
            Vector3Int to = context.PrimaryTile;
            if (from == to)
                return;

            int dx = Mathf.Clamp(to.x - from.x, -1, 1);
            int dy = Mathf.Clamp(to.y - from.y, -1, 1);
            if (dx == 0 && dy == 0)
                return;

            Vector3Int step = new Vector3Int(dx, dy, from.z);
            Vector3Int cell = from + step;
            int steps = 0;

            while (steps < maxLength)
            {
                if (cell == to)
                    break;

                buffer.Add(cell);
                cell += step;
                steps++;
            }
        }

        static bool ContainsCell(List<Vector3Int> cells, Vector3Int cell)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i] == cell)
                    return true;
            }

            return false;
        }
    }
}
