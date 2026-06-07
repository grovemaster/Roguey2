using UnityEngine;

namespace JRogue.World.Generation.Phases
{
    public sealed class PortalSetupPhase : IDungeonGenerationPhase
    {
        public void Execute(DungeonGenerationContext context)
        {
            DungeonFloorDefinition def = context.Definition;
            if (def == null)
                return;

            for (int i = 0; i < def.ArrivalBindings.Count; i++)
            {
                PortalArrivalBinding binding = def.ArrivalBindings[i];
                if (string.IsNullOrEmpty(binding.portalLinkId))
                    continue;

                context.PortalArrivals[binding.portalLinkId] = binding;
                context.Instance.StoreArrivalBinding(binding);
            }

            int placed = 0;
            var usedCells = new System.Collections.Generic.HashSet<Vector3Int>();

            for (int i = 0; i < context.ResolvedPortals.Count; i++)
            {
                ResolvedPortalPlacement resolved = context.ResolvedPortals[i];
                var spec = new DungeonPortalSpec
                {
                    portalLinkId = resolved.portalLinkId,
                    targetFloorId = resolved.targetFloorId,
                    portalCell = resolved.cell,
                    listLabel = resolved.listLabel,
                };
                TryPlacePortal(context, spec, usedCells, ref placed);
            }

            for (int i = 0; i < def.Portals.Count; i++)
                TryPlacePortal(context, def.Portals[i], usedCells, ref placed);

            DungeonGenerationLog.Phase(nameof(PortalSetupPhase),
                $"portals={placed} arrivalBindings={def.ArrivalBindings.Count} resolved={context.ResolvedPortals.Count}");
        }

        static void TryPlacePortal(
            DungeonGenerationContext context,
            DungeonPortalSpec spec,
            System.Collections.Generic.HashSet<Vector3Int> usedCells,
            ref int placed)
        {
            if (spec.adjacentConfirmOnly)
            {
                Vector3Int doorCell = ResolvePortalCell(context, spec);
                if (doorCell == InvalidCell())
                    return;

                usedCells.Add(doorCell);
                context.ReservedCells.Add(doorCell);
                return;
            }

            Vector3Int portalCell = ResolvePortalCell(context, spec);
            if (portalCell == InvalidCell())
                return;

            if (!usedCells.Add(portalCell))
                return;

            context.ReservedCells.Add(portalCell);
            context.AddChebyshevDisk(portalCell, context.Definition.PlayerSafeRadius);

            if (!IsBuildingPortal(spec.portalLinkId))
                context.Instance.PlacePortalVisual(portalCell);

            if (string.IsNullOrEmpty(spec.targetFloorId))
                return;

            var interactable = new PortalInteractable(
                portalCell,
                spec.portalLinkId,
                spec.targetFloorId,
                string.IsNullOrEmpty(spec.listLabel) ? "Portal" : spec.listLabel);

            context.Portals.Add(interactable);
            context.Instance.RegisterPortal(interactable);
            placed++;
        }

        static Vector3Int ResolvePortalCell(DungeonGenerationContext context, DungeonPortalSpec spec)
        {
            Vector3Int resolved = PortalPlacementResolver.ResolveStampPortalCell(
                context.Definition.LayoutStamp,
                spec);
            if (resolved != InvalidCell())
                return resolved;

            return spec.portalCell;
        }

        static Vector3Int InvalidCell() => new Vector3Int(int.MinValue, int.MinValue, 0);

        static bool IsBuildingPortal(string portalLinkId) =>
            !string.IsNullOrEmpty(portalLinkId) && portalLinkId.StartsWith("building_");
    }
}
