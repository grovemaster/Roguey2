using UnityEngine;

namespace JRogue.World.Generation.Vaults
{
    /// <summary>Floor 1 mandatory descent plinth near northern_dark north edge (row y = 79).</summary>
    public static class DescentPlinthPlacementLogic
    {
        public const string VaultId = "vault_descent_plinth_3x3";
        public const string RequiredZoneId = "northern_dark";
        public const int Floor01NorthMapRow = 79;
        public const int MaxChebyshevFromNorthEdge = 3;

        public static bool IsNearNorthMapEdge(Vector3Int cell, int northMapRow, int maxChebyshev) =>
            northMapRow - cell.y <= maxChebyshev && cell.y <= northMapRow;

        public static bool TryGetPortalCell(VaultBlueprint blueprint, Vector3Int origin, out Vector3Int portalCell)
        {
            portalCell = default;
            if (blueprint?.Interactables == null || blueprint.Interactables.Count == 0)
                return false;

            VaultInteractablePlacement interactable = blueprint.Interactables[0];
            portalCell = blueprint.LocalToWorld(origin, interactable.X, interactable.Y);
            return true;
        }

        public static void OnPlaced(DungeonGenerationContext context, VaultBlueprint blueprint, Vector3Int origin)
        {
            if (context == null || blueprint == null || blueprint.VaultId != VaultId)
                return;

            if (!TryGetPortalCell(blueprint, origin, out Vector3Int portalCell))
                return;

            context.DescentPlinthPortalCell = portalCell;
            context.Instance?.SetDescentPlinthPortalCell(portalCell);

            Vector3Int returnArrival = portalCell + Vector3Int.down;
            var binding = new PortalArrivalBinding
            {
                portalLinkId = DungeonFloorTransitionIds.Floor02ToFloor01,
                arrivalAnchor = returnArrival,
            };
            context.PortalArrivals[binding.portalLinkId] = binding;
            context.Instance?.StoreArrivalBinding(binding);
        }
    }
}
