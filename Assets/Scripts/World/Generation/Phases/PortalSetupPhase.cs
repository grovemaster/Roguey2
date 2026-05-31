using System.Collections.Generic;
using JRogue.World.Generation;
using UnityEngine;

namespace JRogue.World.Generation.Phases
{
    public sealed class PortalSetupPhase : IDungeonGenerationPhase
    {
        public void Execute(DungeonGenerationContext context)
        {
            DungeonFloorDefinition def = context.Definition;
            if (def?.Portals == null)
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
            for (int i = 0; i < def.Portals.Count; i++)
            {
                DungeonPortalSpec spec = def.Portals[i];
                Vector3Int portalCell = ResolvePortalCell(def.LayoutStamp, spec);
                if (portalCell == new Vector3Int(int.MinValue, int.MinValue, 0))
                    continue;

                context.ReservedCells.Add(portalCell);
                context.AddChebyshevDisk(portalCell, def.PlayerSafeRadius);

                var interactable = new PortalInteractable(
                    portalCell,
                    spec.portalLinkId,
                    spec.targetFloorId,
                    string.IsNullOrEmpty(spec.listLabel) ? "Portal" : spec.listLabel);

                context.Portals.Add(interactable);
                context.Instance.RegisterPortal(interactable);
                placed++;
            }

            DungeonGenerationLog.Phase(nameof(PortalSetupPhase),
                $"portals={placed} arrivalBindings={def.ArrivalBindings.Count}");
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
