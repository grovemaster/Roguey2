using JRogue.Core.Actor;
using JRogue.Manager.Map;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.Manager.Grid
{
    /// <summary>
    /// Converts grid cell indices to world space using the active floor tilemap when available,
    /// so actors line up with painted tiles.
    /// </summary>
    public static class GridCellWorld
    {
        /// <summary>Matches <see cref="GridFootprintUtility.SingleCellActorInsetRatio"/>.</summary>
        public const float SingleCellActorInsetRatio = GridFootprintUtility.SingleCellActorInsetRatio;

        public static Vector3 GetCellSize()
        {
            Tilemap floor = MapManager.Instance != null ? MapManager.Instance.FloorMap : null;
            UnityEngine.Grid layoutGrid = floor != null ? floor.layoutGrid : null;
            return layoutGrid != null ? layoutGrid.cellSize : Vector3.one;
        }

        public static Vector3 GetCellCenter(Vector3Int cell)
        {
            Tilemap floor = MapManager.Instance != null ? MapManager.Instance.FloorMap : null;
            return GetCellCenter(floor, cell);
        }

        /// <summary>
        /// Geometric cell center in world space. Uses the layout <see cref="Grid"/> when available;
        /// with <see cref="Tilemap.tileAnchor"/> at (0,0), <see cref="Tilemap.GetCellCenterWorld"/>
        /// tracks the anchor (cell corner), not the center.
        /// </summary>
        public static Vector3 GetCellCenter(Tilemap floor, Vector3Int cell)
        {
            if (floor == null)
            {
                Vector3 size = Vector3.one;
                return new Vector3(
                    cell.x + size.x * SingleCellActorInsetRatio,
                    cell.y + size.y * SingleCellActorInsetRatio,
                    0f);
            }

            UnityEngine.Grid grid = floor.layoutGrid;
            if (grid != null)
                return grid.GetCellCenterWorld(cell);

            Vector3 cellSize = floor.cellSize;
            return floor.CellToWorld(cell) + new Vector3(
                cellSize.x * SingleCellActorInsetRatio,
                cellSize.y * SingleCellActorInsetRatio,
                cellSize.z * SingleCellActorInsetRatio);
        }

        /// <summary>
        /// World transform for a 1×1 actor (cell 16,10 → 16.5, 10.5 for 1-unit cells).
        /// </summary>
        public static Vector3 GetSingleCellActorPosition(Vector3Int cell)
        {
            Tilemap floor = MapManager.Instance != null ? MapManager.Instance.FloorMap : null;
            return GetCellCenter(floor, cell);
        }

        public static Vector3Int WorldToCell(Vector3 worldPosition)
        {
            Tilemap floor = MapManager.Instance != null ? MapManager.Instance.FloorMap : null;
            if (floor != null)
                return floor.WorldToCell(worldPosition);

            return new Vector3Int(
                Mathf.FloorToInt(worldPosition.x),
                Mathf.FloorToInt(worldPosition.y),
                0);
        }

        /// <summary>
        /// Inverse of <see cref="GetSingleCellActorPosition"/>.
        /// </summary>
        public static Vector3Int WorldToCellForSingleCellActor(Vector3 actorWorldPosition)
        {
            Tilemap floor = MapManager.Instance != null ? MapManager.Instance.FloorMap : null;
            if (floor != null)
                return floor.WorldToCell(actorWorldPosition);

            Vector3 size = GetCellSize();
            return new Vector3Int(
                Mathf.FloorToInt(actorWorldPosition.x - size.x * SingleCellActorInsetRatio),
                Mathf.FloorToInt(actorWorldPosition.y - size.y * SingleCellActorInsetRatio),
                0);
        }
    }
}
