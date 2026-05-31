using JRogue.Interactables;
using JRogue.World.MapInteract;
using UnityEngine;

namespace JRogue.GridFeatures
{
    /// <summary>
    /// Whether actors may enter a floor cell (levers, altars, etc.).
    /// </summary>
    public static class MapCellOccupancy
    {
        public static bool BlocksActorEntry(Vector3Int cell)
        {
            InteractableTileService levers = InteractableTileService.Instance;
            if (levers != null && levers.BlocksOccupancy(cell))
                return true;

            AdjacentMapInteractableService adjacent = AdjacentMapInteractableService.Instance;
            return adjacent != null && adjacent.BlocksOccupancy(cell);
        }
    }
}
