using System.Collections.Generic;
using UnityEngine;

namespace JRogue.World.Generation.Zones
{
    public static class ZoneSolidPainter
    {
        public static ZonePaintStats PaintSolidLayout(
            JRogue.Manager.Map.MapManager map,
            DungeonFloorDefinition floorDef,
            DungeonFloorZoneLayout layout,
            IReadOnlyList<ResolvedZonePiece> pieces,
            IReadOnlyDictionary<Vector3Int, string> zoneCellMap,
            ZoneTilePaintContext paintContext)
        {
            var stats = new ZonePaintStats
            {
                FloorCellsByZone = new Dictionary<string, int>(),
                WallCellsByZone = new Dictionary<string, int>(),
            };

            if (map == null || layout == null || zoneCellMap == null)
                return stats;

            map.ClearAllTiles();

            int width = layout.FloorWidth;
            int height = layout.FloorHeight;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector3Int cell = new Vector3Int(x, y, 0);
                    bool onOuterEdge = x == 0 || y == 0 || x == width - 1 || y == height - 1;

                    if (!zoneCellMap.TryGetValue(cell, out string zoneId))
                        zoneId = layout.FallbackZoneId;

                    if (onOuterEdge || zoneId == ZoneIds.Empty || zoneId == ZoneIds.Rock || zoneId == layout.FallbackZoneId)
                    {
                        if (onOuterEdge)
                            stats.OuterEdgeWallCells++;

                        ZoneTilePainter.PaintWall(map, cell, layout, floorDef, zoneId, paintContext);
                        Increment(stats.WallCellsByZone, zoneId);
                        continue;
                    }

                    ZoneTilePainter.PaintFloor(map, cell, layout, floorDef, zoneId, paintContext);
                    Increment(stats.FloorCellsByZone, zoneId);
                }
            }

            map.FloorMap?.CompressBounds();
            map.WallMap?.CompressBounds();
            return stats;
        }

        static void Increment(Dictionary<string, int> counts, string zoneId)
        {
            if (string.IsNullOrEmpty(zoneId))
                zoneId = "(null)";

            if (counts.TryGetValue(zoneId, out int count))
                counts[zoneId] = count + 1;
            else
                counts[zoneId] = 1;
        }
    }
}
