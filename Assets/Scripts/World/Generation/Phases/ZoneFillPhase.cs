using System.Collections.Generic;
using JRogue.Manager.Map;
using JRogue.World.Generation.Zones;

namespace JRogue.World.Generation.Phases
{
    public sealed class ZoneFillPhase : IDungeonGenerationPhase
    {
        public void Execute(DungeonGenerationContext context)
        {
            DungeonFloorDefinition def = context.Definition;
            DungeonFloorZoneLayout layout = def?.ZoneLayout;
            if (layout == null)
            {
                DungeonGenerationLog.Error("ZoneFillPhase: missing zoneLayout.");
                return;
            }

            if (context.ZoneCellMap == null || context.ResolvedZonePieces == null)
            {
                DungeonGenerationLog.Error("ZoneFillPhase: ZoneLayoutPhase did not run.");
                return;
            }

            MapManager map = MapManager.Instance;
            if (map == null)
            {
                DungeonGenerationLog.Error("ZoneFillPhase: MapManager.Instance is null.");
                return;
            }

            if (map.FloorMap == null || map.WallMap == null)
            {
                DungeonGenerationLog.Error("ZoneFillPhase: floor/wall tilemaps not bound.");
                return;
            }

            map.ConfigurePaintTiles(def.FloorTile, def.WallTile);
            ZonePaintStats paintStats = ZoneSolidPainter.PaintSolidLayout(
                map,
                def,
                layout,
                context.ResolvedZonePieces,
                context.ZoneCellMap);

            Dictionary<string, int> mapCounts = ZoneCellMapStats.CountByZone(context.ZoneCellMap);
            DungeonGenerationLog.Phase(nameof(ZoneFillPhase),
                $"painted {layout.FloorWidth}x{layout.FloorHeight} solid slots; " +
                $"{ZoneCellMapStats.FormatCounts(mapCounts)}; " +
                $"{ZonePaintStatsFormatter.Format(paintStats)}");
        }
    }
}
