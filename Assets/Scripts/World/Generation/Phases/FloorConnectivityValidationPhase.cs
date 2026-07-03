using JRogue.Manager.Map;
using JRogue.World.Generation.Zones;
using UnityEngine;

namespace JRogue.World.Generation.Phases
{
    /// <summary>
    /// Post-placement check: player spawn must be walkable-reachable to every resolved portal.
    /// </summary>
    public sealed class FloorConnectivityValidationPhase : IDungeonGenerationPhase
    {
        public void Execute(DungeonGenerationContext context)
        {
            if (context?.Definition == null || context.ResolvedPortals.Count == 0)
                return;

            MapManager map = MapManager.Instance;
            if (map == null)
            {
                DungeonGenerationLog.Error($"{nameof(FloorConnectivityValidationPhase)}: MapManager.Instance is null.");
                return;
            }

            if (!map.IsWalkable(context.PlayerStart))
            {
                DungeonGenerationLog.Error(
                    $"{nameof(FloorConnectivityValidationPhase)}: playerStart={context.PlayerStart} is not walkable.");
                return;
            }

            int unreachable = 0;
            for (int i = 0; i < context.ResolvedPortals.Count; i++)
            {
                ResolvedPortalPlacement portal = context.ResolvedPortals[i];
                if (ChebyshevDistance(context.PlayerStart, portal.cell) <= 1)
                {
                    DungeonGenerationLog.Error(
                        $"{nameof(FloorConnectivityValidationPhase)}: playerStart={context.PlayerStart} is adjacent to " +
                        $"portal '{portal.portalLinkId}' at {portal.cell} (R2 violation).");
                }

                if (ZoneHabitatConnectivityEnforcer.TryIsWalkableReachable(
                        map,
                        context.PlayerStart,
                        portal.cell))
                {
                    continue;
                }

                unreachable++;
                DungeonGenerationLog.Error(
                    $"{nameof(FloorConnectivityValidationPhase)}: portal '{portal.portalLinkId}' at {portal.cell} " +
                    $"is not reachable from playerStart={context.PlayerStart}.");
            }

            if (unreachable == 0)
            {
                DungeonGenerationLog.Phase(
                    nameof(FloorConnectivityValidationPhase),
                    $"all {context.ResolvedPortals.Count} portal(s) reachable from playerStart={context.PlayerStart}");
            }
        }

        static int ChebyshevDistance(Vector3Int a, Vector3Int b) =>
            Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
    }
}
