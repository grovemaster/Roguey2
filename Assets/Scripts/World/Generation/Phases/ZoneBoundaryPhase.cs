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

            if (context.ResolvedZoneBoundaries == null || context.ResolvedZoneBoundaries.Count == 0)
            {
                List<ZoneInterface> interfaces = ZoneInterfaceResolver.ResolveInterfaces(
                    context.ResolvedZonePieces,
                    layout.FloorWidth,
                    layout.FloorHeight);
                System.Random boundaryRng = ZoneGenerationRng.CreateZoneSelectionRng(
                    context.RunSeed,
                    def.FloorId + "_boundaries");
                context.ResolvedZoneBoundaries = ZoneBoundaryResolver.ResolveAll(
                    layout,
                    context.ResolvedZonePieces,
                    interfaces,
                    boundaryRng);
            }

            ZoneGenerationDiagnostics.LogBoundaries(context.ResolvedZoneBoundaries);

            ZoneBoundaryStats stats = ZoneBoundaryApplicator.ApplyAll(
                map,
                def,
                layout,
                context.ResolvedZonePieces,
                context.ResolvedZoneBoundaries,
                ZoneTilePaintContext.From(context));

            var log = new StringBuilder();
            log.Append($"interfaces={context.ResolvedZoneBoundaries?.Count ?? 0} boundaries={context.ResolvedZoneBoundaries?.Count ?? 0} ");
            log.Append($"openCells={stats.OpenCells} wallCells={stats.WallCells} ");
            log.Append($"corridorOpenings={stats.CorridorOpenings}");
            DungeonGenerationLog.Phase(nameof(ZoneBoundaryPhase), log.ToString());
            ZoneGenerationDiagnostics.LogCheckpoint(context, "after ZoneBoundaryPhase");
        }
    }
}
