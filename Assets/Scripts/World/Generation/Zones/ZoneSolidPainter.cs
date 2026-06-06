using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.World.Generation.Zones
{
    public static class ZoneSolidPainter
    {
        public static ZonePaintStats PaintSolidLayout(
            JRogue.Manager.Map.MapManager map,
            DungeonFloorDefinition floorDef,
            DungeonFloorZoneLayout layout,
            IReadOnlyList<ResolvedZonePiece> pieces,
            IReadOnlyDictionary<Vector3Int, string> zoneCellMap)
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

                        TileBase wall = ResolveWallTile(layout, floorDef, zoneId);
                        if (wall != null)
                            map.SetCellWall(cell, wall);

                        Increment(stats.WallCellsByZone, zoneId);
                        continue;
                    }

                    if (layout.TryGetZoneDefinition(zoneId, out DungeonZoneDefinition zoneDef)
                        && zoneDef.FloorTile != null)
                    {
                        map.SetCellFloor(cell, zoneDef.FloorTile);
                        Increment(stats.FloorCellsByZone, zoneId);
                    }
                    else if (floorDef?.FloorTile != null)
                    {
                        map.SetCellFloor(cell, floorDef.FloorTile);
                        stats.MissingZoneDefinitionFloorFallback++;
                        Increment(stats.FloorCellsByZone, zoneId + "?");
                    }
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

        static TileBase ResolveWallTile(
            DungeonFloorZoneLayout layout,
            DungeonFloorDefinition floorDef,
            string zoneId)
        {
            if (layout.TryGetZoneDefinition(zoneId, out DungeonZoneDefinition zoneDef) && zoneDef.WallTile != null)
                return zoneDef.WallTile;

            return floorDef?.WallTile;
        }
    }
}
