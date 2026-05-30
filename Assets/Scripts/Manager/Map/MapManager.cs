using JRogue.Manager.Door;
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
        [SerializeField] private Tilemap doorOverlayMap;

        public Tilemap FloorMap => floorMap;
        public Tilemap WallMap => wallMap;
        public Tilemap HazardOverlayMap => hazardOverlayMap;
        public Tilemap InteractableOverlayMap => interactableOverlayMap;
        public Tilemap TrapOverlayMap => trapOverlayMap;
        public Tilemap DoorOverlayMap => doorOverlayMap;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public bool IsCellBlocked(Vector3Int gridPos)
        {
            if (DoorService.Instance != null && DoorService.Instance.BlocksMovement(gridPos))
                return true;

            if (wallMap.HasTile(gridPos))
                return true;

            if (!floorMap.HasTile(gridPos))
                return true;

            return false;
        }

        public bool IsWalkable(Vector3Int gridPos)
        {
            if (!floorMap.HasTile(gridPos))
                return false;

            if (DoorService.Instance != null && DoorService.Instance.BlocksMovement(gridPos))
                return false;

            return !wallMap.HasTile(gridPos);
        }

        public bool IsWall(Vector3Int gridPos)
        {
            return wallMap != null && wallMap.HasTile(gridPos);
        }
    }
}