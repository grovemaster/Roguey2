using JRogue.Manager.Map;
using JRogue.World.Generation;
using JRogue.World.Generation.Zones;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.World.Generation.Phases
{
    public sealed class LayoutStampPhase : IDungeonGenerationPhase
    {
        public void Execute(DungeonGenerationContext context)
        {
            DungeonFloorDefinition def = context?.Definition;
            DungeonLayoutStamp stamp = def?.LayoutStamp;
            if (stamp == null)
            {
                DungeonGenerationLog.Error("LayoutStampPhase: missing layoutStamp on floor definition.");
                return;
            }

            MapManager map = MapManager.Instance;
            if (map == null)
            {
                DungeonGenerationLog.Error("LayoutStampPhase: MapManager.Instance is null.");
                return;
            }

            if (map.FloorMap == null || map.WallMap == null)
            {
                DungeonGenerationLog.Error("LayoutStampPhase: floor/wall tilemaps not bound — call BindToMapManager before generate.");
                return;
            }

            map.ConfigurePaintTiles(def.FloorTile, def.WallTile);

            if (UsesPalettePaint(def))
                PaintStampWithPalettes(map, stamp, def, context);
            else
                map.PaintLayoutStamp(stamp);

            context.PlayerStart = stamp.PlayerStart;
            context.AddChebyshevDisk(context.PlayerStart, def.PlayerSafeRadius);
            DungeonGenerationLog.Phase(nameof(LayoutStampPhase),
                $"painted {stamp.Width}x{stamp.Height} playerStart={context.PlayerStart}");
        }

        static bool UsesPalettePaint(DungeonFloorDefinition def) =>
            (def.DefaultFloorPalette != null && def.DefaultFloorPalette.HasValidEntries)
            || (def.DefaultWallPalette != null && def.DefaultWallPalette.HasValidEntries);

        static void PaintStampWithPalettes(
            MapManager map,
            DungeonLayoutStamp stamp,
            DungeonFloorDefinition def,
            DungeonGenerationContext context)
        {
            var paintContext = ZoneTilePaintContext.From(context);
            const string defaultZoneId = "stamp";

            map.ClearAllTiles();

            for (int y = 0; y < stamp.Height; y++)
            {
                for (int x = 0; x < stamp.Width; x++)
                {
                    Vector3Int cell = new Vector3Int(x, y, 0);
                    if (stamp.IsWall(x, y))
                    {
                        TileBase tile = DungeonTilePaletteResolver.ResolveWallTile(
                            cell,
                            layout: null,
                            def,
                            defaultZoneId,
                            paintContext);
                        if (tile != null)
                            map.SetCellWall(cell, tile);
                    }
                    else if (stamp.IsFloor(x, y))
                    {
                        TileBase tile = DungeonTilePaletteResolver.ResolveFloorTile(
                            cell,
                            layout: null,
                            def,
                            defaultZoneId,
                            paintContext);
                        if (tile != null)
                            map.SetCellFloor(cell, tile);
                    }
                }
            }

            map.FloorMap?.CompressBounds();
            map.WallMap?.CompressBounds();
        }
    }
}
