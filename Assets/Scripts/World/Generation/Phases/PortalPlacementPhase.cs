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
            if (map == null)
                return;

            int mapWidth;
            int mapHeight;
            DungeonLayoutStamp stamp = def.LayoutStamp;
            if (context.UsesZoneComposite)
            {
                if (!PopulationPlacementUtility.TryGetMapBounds(context, out mapWidth, out mapHeight))
                    return;
            }
            else if (stamp == null)
            {
                return;
            }
            else
            {
                mapWidth = stamp.Width;
                mapHeight = stamp.Height;
            }

            int placed = 0;
            MapEdge[] edges = { MapEdge.South, MapEdge.North, MapEdge.East, MapEdge.West };
            int targetCount = Mathf.Clamp(def.OrthogonalEdgePortalCount, 0, edges.Length);

            for (int i = 0; i < targetCount; i++)
            {
                bool found = context.UsesZoneComposite
                    ? PortalEdgePlacement.TryFindEdgePortalCell(
                        mapWidth,
                        mapHeight,
                        map,
                        edges[i],
                        def.OrthogonalEdgeInset,
                        out Vector3Int cell)
                    : PortalEdgePlacement.TryFindEdgePortalCell(
                        stamp,
                        map,
                        edges[i],
                        def.OrthogonalEdgeInset,
                        out cell);
                if (!found)
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
