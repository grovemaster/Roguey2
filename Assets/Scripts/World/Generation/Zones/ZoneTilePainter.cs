using JRogue.World.Generation;
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
            string zoneId,
            ZoneTilePaintContext paintContext)
        {
            if (map == null)
                return;

            TileBase tile = ResolveFloorTile(cell, layout, floorDef, zoneId, paintContext);
            if (tile != null)
                map.SetCellFloor(cell, tile);
        }

        public static void PaintWall(
            JRogue.Manager.Map.MapManager map,
            Vector3Int cell,
            DungeonFloorZoneLayout layout,
            DungeonFloorDefinition floorDef,
            string zoneId,
            ZoneTilePaintContext paintContext)
        {
            if (map == null)
                return;

            TileBase tile = ResolveWallTile(cell, layout, floorDef, zoneId, paintContext);
            if (tile != null)
                map.SetCellWall(cell, tile);
        }

        public static TileBase ResolveFloorTile(
            Vector3Int cell,
            DungeonFloorZoneLayout layout,
            DungeonFloorDefinition floorDef,
            string zoneId,
            ZoneTilePaintContext paintContext) =>
            DungeonTilePaletteResolver.ResolveFloorTile(cell, layout, floorDef, zoneId, paintContext);

        public static TileBase ResolveWallTile(
            Vector3Int cell,
            DungeonFloorZoneLayout layout,
            DungeonFloorDefinition floorDef,
            string zoneId,
            ZoneTilePaintContext paintContext) =>
            DungeonTilePaletteResolver.ResolveWallTile(cell, layout, floorDef, zoneId, paintContext);
    }
}
