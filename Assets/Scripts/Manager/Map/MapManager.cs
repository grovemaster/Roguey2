using JRogue.GridFeatures;
using JRogue.Manager.Door;
using JRogue.World.Generation;
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

        [SerializeField] private TileBase floorPaintTile;
        [SerializeField] private TileBase wallPaintTile;

        public Tilemap FloorMap => floorMap;
        public Tilemap WallMap => wallMap;
        public Tilemap HazardOverlayMap => hazardOverlayMap;
        public Tilemap InteractableOverlayMap => interactableOverlayMap;
        public Tilemap TrapOverlayMap => trapOverlayMap;
        public Tilemap DoorOverlayMap => doorOverlayMap;
        public TileBase FloorPaintTile => floorPaintTile;
        public TileBase WallPaintTile => wallPaintTile;
        public string ActiveFloorId { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public bool IsCellBlocked(Vector3Int gridPos)
        {
            if (floorMap == null)
                return true;

            if (DoorService.Instance != null && DoorService.Instance.BlocksMovement(gridPos))
                return true;

            if (wallMap != null && wallMap.HasTile(gridPos))
                return true;

            if (!floorMap.HasTile(gridPos))
                return true;

            return false;
        }

        public bool IsWalkable(Vector3Int gridPos)
        {
            if (floorMap == null || !floorMap.HasTile(gridPos))
                return false;

            if (DoorService.Instance != null && DoorService.Instance.BlocksMovement(gridPos))
                return false;

            return wallMap == null || !wallMap.HasTile(gridPos);
        }

        public bool IsWall(Vector3Int gridPos)
        {
            return wallMap != null && wallMap.HasTile(gridPos);
        }

        public void ConfigurePaintTiles(TileBase floorTile, TileBase wallTile)
        {
            if (floorTile != null)
                floorPaintTile = floorTile;
            if (wallTile != null)
                wallPaintTile = wallTile;
        }

        public void SetActiveFloor(DungeonFloorTilemaps tilemaps, string floorId)
        {
            if (tilemaps == null)
                return;

            BindTilemaps(
                tilemaps.FloorMap,
                tilemaps.WallMap,
                tilemaps.HazardOverlayMap,
                tilemaps.InteractableOverlayMap,
                tilemaps.TrapOverlayMap,
                tilemaps.DoorOverlayMap);

            ActiveFloorId = floorId;
        }

        public void BindTilemaps(
            Tilemap floor,
            Tilemap wall,
            Tilemap hazard = null,
            Tilemap interactable = null,
            Tilemap trap = null,
            Tilemap door = null)
        {
            floorMap = floor;
            wallMap = wall;
            hazardOverlayMap = hazard;
            interactableOverlayMap = interactable;
            trapOverlayMap = trap;
            doorOverlayMap = door;
        }

        public void ClearAllTiles()
        {
            if (floorMap == null)
            {
                Debug.LogWarning("[MapManager] ClearAllTiles skipped — floorMap not bound. Call SetActiveFloor first.");
                return;
            }

            floorMap.ClearAllTiles();
            wallMap?.ClearAllTiles();
            hazardOverlayMap?.ClearAllTiles();
            interactableOverlayMap?.ClearAllTiles();
            trapOverlayMap?.ClearAllTiles();
            doorOverlayMap?.ClearAllTiles();
        }

        public void SetCellFloor(Vector3Int cell)
        {
            if (floorMap == null || floorPaintTile == null)
                return;

            PaintTileScaledToCell(floorMap, cell, floorPaintTile);
            wallMap?.SetTile(cell, null);
            wallMap?.SetTransformMatrix(cell, Matrix4x4.identity);
        }

        public void SetCellWall(Vector3Int cell)
        {
            if (wallMap == null || wallPaintTile == null)
                return;

            PaintTileScaledToCell(wallMap, cell, wallPaintTile);
            floorMap?.SetTile(cell, null);
            floorMap?.SetTransformMatrix(cell, Matrix4x4.identity);
        }

        static void PaintTileScaledToCell(Tilemap tilemap, Vector3Int cell, TileBase tile)
        {
            tilemap.SetTile(cell, tile);
            if (tile is Tile tileAsset && tileAsset.sprite != null)
                tilemap.SetTransformMatrix(cell, GridOverlayPainter.CreateCellFillMatrix(tilemap, tileAsset.sprite));
            else
                tilemap.SetTransformMatrix(cell, Matrix4x4.identity);
        }

        public void PaintLayoutStamp(DungeonLayoutStamp stamp)
        {
            if (stamp == null)
                return;

            ClearAllTiles();

            for (int y = 0; y < stamp.Height; y++)
            {
                for (int x = 0; x < stamp.Width; x++)
                {
                    Vector3Int cell = new Vector3Int(x, y, 0);
                    if (stamp.IsWall(x, y))
                        SetCellWall(cell);
                    else if (stamp.IsFloor(x, y))
                        SetCellFloor(cell);
                }
            }

            floorMap?.CompressBounds();
            wallMap?.CompressBounds();
        }
    }
}