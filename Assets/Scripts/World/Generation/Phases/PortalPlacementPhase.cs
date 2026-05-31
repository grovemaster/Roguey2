using System.Collections.Generic;
using JRogue.Manager.Map;
using UnityEngine;

namespace JRogue.World.Generation.Phases
{
    /// <summary>
    /// Resolves portal cells from orthogonal edge heuristics before <see cref="PortalSetupPhase"/>.
    /// </summary>
    public sealed class PortalPlacementPhase : IDungeonGenerationPhase
    {
        public void Execute(DungeonGenerationContext context)
        {
            DungeonFloorDefinition def = context.Definition;
            if (def == null || def.OrthogonalEdgePortalCount <= 0)
                return;

            MapManager map = MapManager.Instance;
            DungeonLayoutStamp stamp = def.LayoutStamp;
            if (map == null || stamp == null)
                return;

            int placed = 0;
            MapEdge[] edges = { MapEdge.South, MapEdge.North, MapEdge.East, MapEdge.West };
            int targetCount = Mathf.Clamp(def.OrthogonalEdgePortalCount, 0, edges.Length);

            for (int i = 0; i < targetCount; i++)
            {
                if (!PortalEdgePlacement.TryFindEdgePortalCell(stamp, map, edges[i], def.OrthogonalEdgeInset, out Vector3Int cell))
                    continue;

                if (context.ReservedCells.Contains(cell))
                    continue;

                context.ResolvedEdgePortals.Add(new ResolvedEdgePortal
                {
                    cell = cell,
                    edge = edges[i],
                });
                context.ReservedCells.Add(cell);
                placed++;
            }

            DungeonGenerationLog.Phase(nameof(PortalPlacementPhase), $"edgePortals={placed}");
        }
    }
}
