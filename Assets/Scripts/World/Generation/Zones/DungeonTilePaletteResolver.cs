using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.World.Generation.Zones
{
    public static class DungeonTilePaletteResolver
    {
        public const int FloorLayerSalt = 0;
        public const int WallLayerSalt = 1;

        public static TileBase ResolveFloorTile(
            Vector3Int cell,
            DungeonFloorZoneLayout layout,
            DungeonFloorDefinition floorDef,
            string zoneId,
            ZoneTilePaintContext paintContext)
        {
            return PickFromSources(
                cell,
                zoneId,
                paintContext,
                FloorLayerSalt,
                ResolveFloorPalette(layout, floorDef, zoneId),
                ResolveLegacyFloorTile(layout, floorDef, zoneId));
        }

        public static TileBase ResolveWallTile(
            Vector3Int cell,
            DungeonFloorZoneLayout layout,
            DungeonFloorDefinition floorDef,
            string zoneId,
            ZoneTilePaintContext paintContext)
        {
            return PickFromSources(
                cell,
                zoneId,
                paintContext,
                WallLayerSalt,
                ResolveWallPalette(layout, floorDef, zoneId),
                ResolveLegacyWallTile(layout, floorDef, zoneId));
        }

        static TileBase PickFromSources(
            Vector3Int cell,
            string zoneId,
            ZoneTilePaintContext paintContext,
            int layerSalt,
            DungeonTilePalette palette,
            TileBase legacyTile)
        {
            if (palette != null && palette.HasValidEntries)
                return palette.PickTile(cell, zoneId, paintContext, layerSalt);

            return legacyTile;
        }

        public static DungeonTilePalette ResolveFloorPalette(
            DungeonFloorZoneLayout layout,
            DungeonFloorDefinition floorDef,
            string zoneId)
        {
            if (layout != null
                && layout.TryGetZoneDefinition(zoneId, out DungeonZoneDefinition zoneDef)
                && zoneDef.FloorTilePalette != null
                && zoneDef.FloorTilePalette.HasValidEntries)
            {
                return zoneDef.FloorTilePalette;
            }

            if (floorDef?.DefaultFloorPalette != null && floorDef.DefaultFloorPalette.HasValidEntries)
                return floorDef.DefaultFloorPalette;

            return null;
        }

        public static DungeonTilePalette ResolveWallPalette(
            DungeonFloorZoneLayout layout,
            DungeonFloorDefinition floorDef,
            string zoneId)
        {
            if (layout != null
                && layout.TryGetZoneDefinition(zoneId, out DungeonZoneDefinition zoneDef)
                && zoneDef.WallTilePalette != null
                && zoneDef.WallTilePalette.HasValidEntries)
            {
                return zoneDef.WallTilePalette;
            }

            if (floorDef?.DefaultWallPalette != null && floorDef.DefaultWallPalette.HasValidEntries)
                return floorDef.DefaultWallPalette;

            return null;
        }

        static TileBase ResolveLegacyFloorTile(
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

        static TileBase ResolveLegacyWallTile(
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

        public static int ComputeCellHash(
            ZoneTilePaintContext paintContext,
            string zoneId,
            Vector3Int cell,
            int layerSalt)
        {
            int hash = paintContext.RunSeed;
            hash = CombineHash(hash, paintContext.FloorSalt);
            hash = CombineHash(hash, zoneId != null ? zoneId.GetHashCode() : 0);
            hash = CombineHash(hash, cell.x);
            hash = CombineHash(hash, cell.y);
            hash = CombineHash(hash, layerSalt);
            return hash;
        }

        static int CombineHash(int a, int b) => unchecked((a * 397) ^ b);
    }
}
