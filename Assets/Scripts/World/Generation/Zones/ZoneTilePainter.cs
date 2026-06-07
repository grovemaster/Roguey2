using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.World.Generation.Zones
{
    public static class ZoneTilePainter
    {
        public static bool IsMapOuterEdge(int x, int y, int mapWidth, int mapHeight) =>
            x <= 0 || y <= 0 || x >= mapWidth - 1 || y >= mapHeight - 1;

        public static void PaintFloor(
            JRogue.Manager.Map.MapManager map,
            Vector3Int cell,
            DungeonFloorZoneLayout layout,
            DungeonFloorDefinition floorDef,
            string zoneId)
        {
            if (map == null)
                return;

            TileBase tile = ResolveFloorTile(layout, floorDef, zoneId);
            if (tile != null)
                map.SetCellFloor(cell, tile);
        }

        public static void PaintWall(
            JRogue.Manager.Map.MapManager map,
            Vector3Int cell,
            DungeonFloorZoneLayout layout,
            DungeonFloorDefinition floorDef,
            string zoneId)
        {
            if (map == null)
                return;

            TileBase tile = ResolveWallTile(layout, floorDef, zoneId);
            if (tile != null)
                map.SetCellWall(cell, tile);
        }

        public static TileBase ResolveFloorTile(
            DungeonFloorZoneLayout layout,
            DungeonFloorDefinition floorDef,
            string zoneId)
        {
            if (layout != null
                && layout.TryGetZoneDefinition(zoneId, out DungeonZoneDefinition zoneDef)
                && zoneDef.FloorTile != null)
            {
                return zoneDef.FloorTile;
            }

            return floorDef?.FloorTile;
        }

        public static TileBase ResolveWallTile(
            DungeonFloorZoneLayout layout,
            DungeonFloorDefinition floorDef,
            string zoneId)
        {
            if (layout != null
                && layout.TryGetZoneDefinition(zoneId, out DungeonZoneDefinition zoneDef)
                && zoneDef.WallTile != null)
            {
                return zoneDef.WallTile;
            }

            return floorDef?.WallTile;
        }
    }
}
