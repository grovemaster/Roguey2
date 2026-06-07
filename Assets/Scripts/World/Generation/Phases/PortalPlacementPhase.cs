using JRogue.Manager.Map;
using UnityEngine;

namespace JRogue.World.Generation.Phases
{
    /// <summary>
    /// Resolves portal cells from orthogonal edge, stamp marker, and tagged-region rules
    /// before <see cref="PortalSetupPhase"/>.
    /// </summary>
    public sealed class PortalPlacementPhase : IDungeonGenerationPhase
    {
        public void Execute(DungeonGenerationContext context)
        {
            DungeonFloorDefinition def = context?.Definition;
            if (def == null)
                return;

            if (MapManager.Instance == null && !HasStampOnlyRules(def))
                return;

            int legacyOrthogonal = PortalPlacementResolver.PlaceLegacyOrthogonalEdgePortals(context);
            int rulePlaced = 0;
            for (int i = 0; i < def.PortalPlacementRules.Count; i++)
            {
                if (PortalPlacementResolver.TryPlaceRule(context, def.PortalPlacementRules[i]))
                    rulePlaced++;
            }

            SyncLegacyEdgePortalList(context);

            DungeonGenerationLog.Phase(
                nameof(PortalPlacementPhase),
                $"resolved={context.ResolvedPortals.Count} legacyOrthogonal={legacyOrthogonal} rules={rulePlaced}");
        }

        static bool HasStampOnlyRules(DungeonFloorDefinition def)
        {
            for (int i = 0; i < def.PortalPlacementRules.Count; i++)
            {
                if (def.PortalPlacementRules[i].kind == PortalPlacementRuleKind.FixedStampMarker)
                    return true;
            }

            return false;
        }

        static void SyncLegacyEdgePortalList(DungeonGenerationContext context)
        {
            context.ResolvedEdgePortals.Clear();
            for (int i = 0; i < context.ResolvedPortals.Count; i++)
            {
                ResolvedPortalPlacement portal = context.ResolvedPortals[i];
                if (portal.sourceKind != PortalPlacementRuleKind.OrthogonalMapEdge)
                    continue;

                context.ResolvedEdgePortals.Add(new ResolvedEdgePortal
                {
                    cell = portal.cell,
                    edge = portal.edge,
                });
            }
        }
    }
}
