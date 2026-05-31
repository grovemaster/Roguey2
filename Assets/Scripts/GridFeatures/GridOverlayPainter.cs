using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.GridFeatures
{
    /// <summary>Shared tilemap overlay painting for hazards, traps, interactables.</summary>
    public static class GridOverlayPainter
    {
        public const int DefaultOverlaySortingOrder = 3;

        public static void ConfigureRenderer(Tilemap overlayMap, int sortingOrder = DefaultOverlaySortingOrder)
        {
            if (overlayMap == null)
                return;

            TilemapRenderer renderer = overlayMap.GetComponent<TilemapRenderer>();
            if (renderer != null)
                renderer.sortingOrder = sortingOrder;
        }
        public static void Paint(Tilemap overlayMap, Vector3Int cell, Tile tile, Sprite sprite)
        {
            if (overlayMap == null)
                return;

            if (tile != null)
            {
                overlayMap.SetTransformMatrix(cell, Matrix4x4.identity);
                overlayMap.SetTile(cell, tile);
                return;
            }

            if (sprite != null)
            {
                var runtimeTile = ScriptableObject.CreateInstance<Tile>();
                runtimeTile.sprite = sprite;
                overlayMap.SetTile(cell, runtimeTile);
                overlayMap.SetTransformMatrix(cell, CreateCellFillMatrix(overlayMap, sprite));
                return;
            }

            Clear(overlayMap, cell);
        }

        public static void Clear(Tilemap overlayMap, Vector3Int cell)
        {
            if (overlayMap == null)
                return;

            overlayMap.SetTransformMatrix(cell, Matrix4x4.identity);
            overlayMap.SetTile(cell, null);
        }

        public static Matrix4x4 CreateCellFillMatrix(Tilemap overlayMap, Sprite sprite)
        {
            if (overlayMap == null || sprite == null)
                return Matrix4x4.identity;

            Grid grid = overlayMap.layoutGrid;
            if (grid == null)
                return Matrix4x4.identity;

            float targetSize = grid.cellSize.x;
            float spriteWorldSize = sprite.rect.width / sprite.pixelsPerUnit;
            if (spriteWorldSize <= 0f || targetSize <= 0f)
                return Matrix4x4.identity;

            float scale = targetSize / spriteWorldSize;
            if (Mathf.Abs(scale - 1f) < 0.001f)
                return Matrix4x4.identity;

            return Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        }
    }
}
