using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.Manager.Map
{
    public class MapManager : MonoBehaviour
    {
        public static MapManager Instance { get; private set; } // Add Singleton

        [SerializeField] private Tilemap floorMap;
        [SerializeField] private Tilemap wallMap;
        [SerializeField] private Tilemap hazardOverlayMap;
        [SerializeField] private Tilemap interactableOverlayMap;
        [SerializeField] private Tilemap trapOverlayMap;

        public Tilemap FloorMap => floorMap;
        public Tilemap WallMap => wallMap;
        public Tilemap HazardOverlayMap => hazardOverlayMap;
        public Tilemap InteractableOverlayMap => interactableOverlayMap;
        public Tilemap TrapOverlayMap => trapOverlayMap;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public bool IsCellBlocked(Vector3Int gridPos)
        {
            // 1. Check if the tile exists in the Wall layer
            if (wallMap.HasTile(gridPos)) return true;

            // 2. Check if the floor tile is missing (the "void")
            if (!floorMap.HasTile(gridPos)) return true;

            return false;
        }

        public bool IsWalkable(Vector3Int gridPos)
        {
            // Check: Must have a floor AND NOT have a wall
            return floorMap.HasTile(gridPos) && !wallMap.HasTile(gridPos);
        }

        public bool IsWall(Vector3Int gridPos)
        {
            return wallMap != null && wallMap.HasTile(gridPos);
        }
    }
}