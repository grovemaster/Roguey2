using JRogue.Manager.Map;
using JRogue.World.Generation.Vaults;
using JRogue.World.Generation.Zones;
using UnityEngine;

namespace JRogue.World.Generation.Phases
{
    /// <summary>
    /// Post-placement check: player spawn must be walkable-reachable to every resolved portal
    /// and to the Floor 1 descent plinth when present.
    /// </summary>
    public sealed class FloorConnectivityValidationPhase : IDungeonGenerationPhase
    {
        public void Execute(DungeonGenerationContext context)
        {
            if (context?.Definition == null)
                return;

            if (context.ResolvedPortals.Count == 0 && !context.DescentPlinthPortalCell.HasValue)
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

            if (context.DescentPlinthPortalCell.HasValue)
                unreachable += ValidateDescentPlinthReachability(context, map);

            int portalCount = context.ResolvedPortals.Count + (context.DescentPlinthPortalCell.HasValue ? 1 : 0);
            if (unreachable == 0)
            {
                DungeonGenerationLog.Phase(
                    nameof(FloorConnectivityValidationPhase),
                    $"all {portalCount} portal target(s) reachable from playerStart={context.PlayerStart}");
            }
        }

        static int ValidateDescentPlinthReachability(DungeonGenerationContext context, MapManager map)
        {
            Vector3Int plinthCell = context.DescentPlinthPortalCell.Value;
            if (ChebyshevDistance(context.PlayerStart, plinthCell) <= 1)
            {
                DungeonGenerationLog.Error(
                    $"{nameof(FloorConnectivityValidationPhase)}: playerStart={context.PlayerStart} is adjacent to " +
                    $"descent plinth at {plinthCell} (R2 violation).");
            }

            if (ZoneHabitatConnectivityEnforcer.TryIsWalkableReachable(map, context.PlayerStart, plinthCell))
                return 0;

            DungeonGenerationLog.Error(
                $"{nameof(FloorConnectivityValidationPhase)}: descent plinth at {plinthCell} " +
                $"is not reachable from playerStart={context.PlayerStart}.");
            return 1;
        }

        static int ChebyshevDistance(Vector3Int a, Vector3Int b) =>
            Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
    }
}
