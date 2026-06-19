using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.GridFeatures
{
    /// <summary>Shared tilemap overlay painting for hazards, traps, interactables.</summary>
    public static class GridOverlayPainter
    {
        public const int DefaultOverlaySortingOrder = 3;

        static readonly Vector3 CenterPivotCellTranslate = new Vector3(0.5f, 0.5f, 0f);

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
                overlayMap.SetTile(cell, tile);
                overlayMap.SetTransformMatrix(cell, GetPaintMatrix(overlayMap, null, fillScale: 1f));
                return;
            }

            if (sprite != null)
            {
                var runtimeTile = ScriptableObject.CreateInstance<Tile>();
                runtimeTile.sprite = sprite;
                overlayMap.SetTile(cell, runtimeTile);
                overlayMap.SetTransformMatrix(cell, GetPaintMatrix(overlayMap, sprite, fillScale: 1f));
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

        /// <summary>
        /// Matrix for overlay tiles/sprites aligned with floor paint (anchor at corner + center-pivot translate).
        /// </summary>
        public static Matrix4x4 GetPaintMatrix(Tilemap overlayMap, Sprite sprite, float fillScale = 1f)
        {
            Matrix4x4 fill = CreateCellFillScaleMatrix(overlayMap, sprite, fillScale);
            if (overlayMap == null || overlayMap.tileAnchor != Vector3.zero)
                return fill;

            Matrix4x4 translate = Matrix4x4.TRS(CenterPivotCellTranslate, Quaternion.identity, Vector3.one);
            return translate * fill;
        }

        /// <summary>
        /// Scales a sprite to fill one grid cell (scale only; combine via <see cref="GetPaintMatrix"/>).
        /// </summary>
        public static Matrix4x4 CreateCellFillScaleMatrix(Tilemap overlayMap, Sprite sprite, float fillScale = 1f)
        {
            if (overlayMap == null || sprite == null)
                return Matrix4x4.identity;

            Grid grid = overlayMap.layoutGrid;
            if (grid == null)
                return Matrix4x4.identity;

            Vector3 cellSize = grid.cellSize;
            float targetW = cellSize.x * fillScale;
            float targetH = cellSize.y * fillScale;
            float spriteWorldW = sprite.rect.width / sprite.pixelsPerUnit;
            float spriteWorldH = sprite.rect.height / sprite.pixelsPerUnit;
            if (spriteWorldW <= 0f || spriteWorldH <= 0f || targetW <= 0f || targetH <= 0f)
                return Matrix4x4.identity;

            float scaleX = targetW / spriteWorldW;
            float scaleY = targetH / spriteWorldH;
            if (Mathf.Abs(scaleX - 1f) < 0.001f && Mathf.Abs(scaleY - 1f) < 0.001f)
                return Matrix4x4.identity;

            Vector3 anchor = overlayMap.tileAnchor;
            if (anchor.x == 0f && anchor.y == 0f)
                return Matrix4x4.Scale(new Vector3(scaleX, scaleY, 1f));

            float offsetX = cellSize.x * (0.5f - anchor.x) * (1f - scaleX);
            float offsetY = cellSize.y * (0.5f - anchor.y) * (1f - scaleY);
            return Matrix4x4.TRS(
                new Vector3(offsetX, offsetY, 0f),
                Quaternion.identity,
                new Vector3(scaleX, scaleY, 1f));
        }

        /// <summary>
        /// Scales a sprite to exactly fill one grid cell. Works with <see cref="Tilemap.tileAnchor"/> at (0,0)
        /// so neighboring cells share edges with no dark gaps.
        /// </summary>
        public static Matrix4x4 CreateCellFillMatrix(Tilemap overlayMap, Sprite sprite, float fillScale = 1f) =>
            GetPaintMatrix(overlayMap, sprite, fillScale);

        /// <summary>
        /// Reapplies the center-pivot translate on every painted cell. Use after editor
        /// <see cref="Tilemap.SetTile"/> calls that skip <see cref="MapManager"/> paint helpers.
        /// </summary>
        public static void ApplyCenterPivotAlignmentToPaintedCells(Tilemap tilemap)
        {
            if (tilemap == null)
                return;

            Matrix4x4 matrix = GetPaintMatrix(tilemap, null, fillScale: 1f);
            BoundsInt bounds = tilemap.cellBounds;
            for (int z = bounds.zMin; z < bounds.zMax; z++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    for (int x = bounds.xMin; x < bounds.xMax; x++)
                    {
                        Vector3Int cell = new Vector3Int(x, y, z);
                        if (tilemap.HasTile(cell))
                            tilemap.SetTransformMatrix(cell, matrix);
                    }
                }
            }
        }
    }
}
