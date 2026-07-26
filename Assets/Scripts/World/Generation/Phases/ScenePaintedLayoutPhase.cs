using JRogue.World.Town;
using UnityEngine;

namespace JRogue.World.Generation.Phases
{
    /// <summary>Scene-painted floors keep authored tilemaps; this phase only resolves hub markers.</summary>
    public sealed class ScenePaintedLayoutPhase : IDungeonGenerationPhase
    {
        public void Execute(DungeonGenerationContext context)
        {
            DungeonFloorDefinition def = context?.Definition;
            if (def == null || def.LayoutMode != FloorLayoutMode.ScenePainted)
                return;

            Transform floorRoot = context.Instance != null ? context.Instance.transform : null;
            if (ScenePaintedMarkerUtility.TryGetCell(floorRoot, StaticHubMarkerKind.PlayerStart, out Vector3Int playerStart))
                context.PlayerStart = playerStart;

            if (context.Instance != null && !context.Instance.HasPaintedFloorTiles())
                ScenePaintedFloorGuard.LogMissing(def.FloorId, "Generated an empty runtime instance for");

            DungeonGenerationLog.Phase(
                nameof(ScenePaintedLayoutPhase),
                $"playerStart={context.PlayerStart} (tiles unchanged)");
        }
    }
}
