using System.Collections.Generic;
using System.Text;
using JRogue.Manager.Map;
using JRogue.World.Generation.Zones;

namespace JRogue.World.Generation.Phases
{
    public sealed class ZoneBoundaryPhase : IDungeonGenerationPhase
    {
        public void Execute(DungeonGenerationContext context)
        {
            DungeonFloorDefinition def = context.Definition;
            DungeonFloorZoneLayout layout = def?.ZoneLayout;
            if (layout == null)
            {
                DungeonGenerationLog.Error("ZoneBoundaryPhase: missing zoneLayout.");
                return;
            }

            if (context.ResolvedZonePieces == null)
            {
                DungeonGenerationLog.Error("ZoneBoundaryPhase: ZoneLayoutPhase did not run.");
                return;
            }

            MapManager map = MapManager.Instance;
            if (map == null)
            {
                DungeonGenerationLog.Error("ZoneBoundaryPhase: MapManager.Instance is null.");
                return;
            }

            List<ZoneInterface> interfaces = ZoneInterfaceResolver.ResolveInterfaces(
                context.ResolvedZonePieces,
                layout.FloorWidth,
                layout.FloorHeight);

            List<ResolvedZoneBoundary> boundaries = ZoneBoundaryResolver.ResolveAll(
                layout,
                context.ResolvedZonePieces,
                interfaces);

            ZoneGenerationDiagnostics.LogBoundaries(boundaries);

            ZoneBoundaryStats stats = ZoneBoundaryApplicator.ApplyAll(
                map,
                def,
                layout,
                context.ResolvedZonePieces,
                boundaries);

            var log = new StringBuilder();
            log.Append($"interfaces={interfaces.Count} boundaries={boundaries.Count} ");
            log.Append($"openCells={stats.OpenCells} wallCells={stats.WallCells} ");
            log.Append($"corridorOpenings={stats.CorridorOpenings}");
            DungeonGenerationLog.Phase(nameof(ZoneBoundaryPhase), log.ToString());
            ZoneGenerationDiagnostics.LogCheckpoint(context, "after ZoneBoundaryPhase");
        }
    }
}
