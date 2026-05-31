using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.World.Generation
{
    public sealed class DungeonFloorTilemaps
    {
        public Tilemap FloorMap { get; }
        public Tilemap WallMap { get; }
        public Tilemap HazardOverlayMap { get; }
        public Tilemap InteractableOverlayMap { get; }
        public Tilemap TrapOverlayMap { get; }
        public Tilemap DoorOverlayMap { get; }

        public DungeonFloorTilemaps(
            Tilemap floor,
            Tilemap wall,
            Tilemap hazard = null,
            Tilemap interactable = null,
            Tilemap trap = null,
            Tilemap door = null)
        {
            FloorMap = floor;
            WallMap = wall;
            HazardOverlayMap = hazard;
            InteractableOverlayMap = interactable;
            TrapOverlayMap = trap;
            DoorOverlayMap = door;
        }
    }
}
