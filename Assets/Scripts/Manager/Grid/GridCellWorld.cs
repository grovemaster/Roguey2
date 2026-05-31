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
        /// <summary>
        /// Actor snap offset from the cell origin (corner). Matches <see cref="GridFootprintUtility.SingleCellActorInsetRatio"/>.
        /// </summary>
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
            if (floor != null)
                return floor.GetCellCenterWorld(cell);

            Vector3 size = Vector3.one;
            return new Vector3(
                cell.x + size.x * 0.5f,
                cell.y + size.y * 0.5f,
                0f);
        }

        static Vector3 GetActorInset(Vector3 cellSize) =>
            new Vector3(
                cellSize.x * SingleCellActorInsetRatio,
                cellSize.y * SingleCellActorInsetRatio,
                0f);

        /// <summary>
        /// World transform for a 1×1 actor on a floor tile (cell 16,10 → 16.75, 10.75 for 1-unit cells).
        /// Uses cell origin + inset, not GetCellCenterWorld, to avoid tile-anchor double offsets.
        /// </summary>
        public static Vector3 GetSingleCellActorPosition(Vector3Int cell)
        {
            Vector3 size = GetCellSize();
            Vector3 inset = GetActorInset(size);

            Tilemap floor = MapManager.Instance != null ? MapManager.Instance.FloorMap : null;
            if (floor != null)
                return floor.CellToWorld(cell) + inset;

            return new Vector3(cell.x + inset.x, cell.y + inset.y, 0f);
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
            Vector3 size = GetCellSize();
            Vector3 inset = GetActorInset(size);

            Tilemap floor = MapManager.Instance != null ? MapManager.Instance.FloorMap : null;
            if (floor != null)
                return floor.WorldToCell(actorWorldPosition - inset);

            return new Vector3Int(
                Mathf.FloorToInt(actorWorldPosition.x - inset.x),
                Mathf.FloorToInt(actorWorldPosition.y - inset.y),
                0);
        }
    }
}
