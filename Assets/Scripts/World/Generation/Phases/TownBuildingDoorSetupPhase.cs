using UnityEngine;

namespace JRogue.World.Generation.Phases
{
    /// <summary>Registers Confirm-adjacent building doors from floor portal specs.</summary>
    public sealed class TownBuildingDoorSetupPhase : IDungeonGenerationPhase
    {
        public void Execute(DungeonGenerationContext context)
        {
            DungeonFloorDefinition def = context.Definition;
            if (def == null)
                return;

            int placed = 0;
            for (int i = 0; i < def.Portals.Count; i++)
            {
                DungeonPortalSpec spec = def.Portals[i];
                if (!spec.adjacentConfirmOnly || string.IsNullOrEmpty(spec.targetFloorId))
                    continue;

                Vector3Int cell = PortalPlacementResolver.ResolveStampPortalCell(def.LayoutStamp, spec);
                if (cell.x == int.MinValue)
                {
                    DungeonGenerationLog.Warn(
                        $"{nameof(TownBuildingDoorSetupPhase)} could not resolve door cell for '{spec.portalLinkId}'.");
                    continue;
                }

                var door = new TownBuildingDoorInteractable(
                    cell,
                    spec.portalLinkId,
                    spec.targetFloorId,
                    spec.listLabel);
                context.Instance.RegisterMapInteractable(door);
                placed++;
            }

            if (placed > 0)
            {
                DungeonGenerationLog.Phase(
                    nameof(TownBuildingDoorSetupPhase),
                    $"doors={placed}");
            }
        }
    }
}
