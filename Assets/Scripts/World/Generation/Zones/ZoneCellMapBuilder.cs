using System.Collections.Generic;
using UnityEngine;

namespace JRogue.World.Generation.Zones
{
    public static class ZoneCellMapBuilder
    {
        public static Dictionary<Vector3Int, string> Build(
            int floorWidth,
            int floorHeight,
            string fallbackZoneId,
            IReadOnlyList<ResolvedZonePiece> pieces)
        {
            var map = new Dictionary<Vector3Int, string>();
            string fallback = string.IsNullOrEmpty(fallbackZoneId) ? ZoneIds.Rock : fallbackZoneId;

            for (int y = 0; y < floorHeight; y++)
            {
                for (int x = 0; x < floorWidth; x++)
                {
                    Vector3Int cell = new Vector3Int(x, y, 0);
                    map[cell] = fallback;
                }
            }

            if (pieces == null)
                return map;

            for (int i = 0; i < pieces.Count; i++)
            {
                ResolvedZonePiece piece = pieces[i];
                if (piece.ZoneId == ZoneIds.Empty)
                    continue;

                RectInt bounds = piece.Bounds;
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    for (int x = bounds.xMin; x < bounds.xMax; x++)
                    {
                        Vector3Int cell = new Vector3Int(x, y, 0);
                        map[cell] = piece.ZoneId;
                    }
                }
            }

            return map;
        }

        public static Dictionary<string, RectInt> BuildZoneBounds(IReadOnlyList<ResolvedZonePiece> pieces)
        {
            var bounds = new Dictionary<string, RectInt>();
            if (pieces == null)
                return bounds;

            for (int i = 0; i < pieces.Count; i++)
            {
                ResolvedZonePiece piece = pieces[i];
                if (piece.ZoneId == ZoneIds.Empty)
                    continue;

                if (bounds.TryGetValue(piece.ZoneInstanceId, out RectInt existing))
                {
                    int xMin = Mathf.Min(existing.xMin, piece.Bounds.xMin);
                    int yMin = Mathf.Min(existing.yMin, piece.Bounds.yMin);
                    int xMax = Mathf.Max(existing.xMax, piece.Bounds.xMax);
                    int yMax = Mathf.Max(existing.yMax, piece.Bounds.yMax);
                    bounds[piece.ZoneInstanceId] = ZoneCompassRectResolver.FromInclusiveBounds(
                        xMin,
                        yMin,
                        xMax,
                        yMax);
                }
                else
                {
                    bounds[piece.ZoneInstanceId] = piece.Bounds;
                }
            }

            return bounds;
        }
    }
}
