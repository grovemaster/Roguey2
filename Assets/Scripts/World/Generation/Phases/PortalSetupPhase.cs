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

            for (int i = 0; i < def.Portals.Count; i++)
                TryPlacePortal(context, def.Portals[i], usedCells, ref placed);

            for (int i = 0; i < context.ResolvedEdgePortals.Count; i++)
            {
                ResolvedEdgePortal edgePortal = context.ResolvedEdgePortals[i];
                if (!usedCells.Add(edgePortal.cell))
                    continue;

                def.TryGetEdgePortalSpec(edgePortal.edge, out EdgePortalSpec edgeSpec);
                if (!string.IsNullOrEmpty(edgeSpec.targetFloorId))
                {
                    var spec = new DungeonPortalSpec
                    {
                        portalLinkId = edgeSpec.portalLinkId,
                        targetFloorId = edgeSpec.targetFloorId,
                        portalCell = edgePortal.cell,
                        listLabel = edgeSpec.listLabel,
                    };
                    TryPlacePortal(context, spec, usedCells, ref placed);
                }
                else
                {
                    context.Instance.PlacePortalVisual(edgePortal.cell);
                }
            }

            DungeonGenerationLog.Phase(nameof(PortalSetupPhase),
                $"portals={placed} arrivalBindings={def.ArrivalBindings.Count}");
        }

        static void TryPlacePortal(
            DungeonGenerationContext context,
            DungeonPortalSpec spec,
            System.Collections.Generic.HashSet<Vector3Int> usedCells,
            ref int placed)
        {
            Vector3Int portalCell = ResolvePortalCell(context.Definition.LayoutStamp, spec);
            if (portalCell == new Vector3Int(int.MinValue, int.MinValue, 0))
                return;

            if (!usedCells.Add(portalCell))
                return;

            context.ReservedCells.Add(portalCell);
            context.AddChebyshevDisk(portalCell, context.Definition.PlayerSafeRadius);

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

        static Vector3Int ResolvePortalCell(DungeonLayoutStamp stamp, DungeonPortalSpec spec)
        {
            if (!string.IsNullOrEmpty(spec.portalMarkerId) && stamp != null &&
                stamp.TryGetMarker(spec.portalMarkerId, out Vector3Int markerCell))
                return markerCell;

            return spec.portalCell;
        }
    }
}
