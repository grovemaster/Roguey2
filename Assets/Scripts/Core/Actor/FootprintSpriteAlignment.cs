using System.Collections.Generic;
using UnityEngine;

namespace JRogue.Core.Actor
{
    /// <summary>
    /// Tests whether a bottom-left-pivot sprite quad covers the same grid cells as a footprint.
    /// Used by unit tests and editor diagnostics.
    /// </summary>
    public static class FootprintSpriteAlignment
    {
        public const float DefaultSpriteUnitSize = 1f;
        public static readonly Vector2 BottomLeftPivot = Vector2.zero;

        public static Vector3 GetAnchorWorldPosition(Vector3Int anchor) =>
            new Vector3(anchor.x, anchor.y, 0f);

        /// <summary>
        /// World-axis rectangle covered by a sprite with pivot01 (0,0) = bottom-left at <paramref name="pivotWorldPosition"/>.
        /// </summary>
        public static Rect GetSpriteWorldRect(
            Vector3 pivotWorldPosition,
            Vector2 pivot01,
            Vector2 scale,
            float unitSize = DefaultSpriteUnitSize)
        {
            float w = unitSize * scale.x;
            float h = unitSize * scale.y;
            float minX = pivotWorldPosition.x - w * pivot01.x;
            float minY = pivotWorldPosition.y - h * pivot01.y;
            return new Rect(minX, minY, w, h);
        }

        public static int CountCellOverlap(Rect spriteRect, IReadOnlyList<Vector3Int> footprintCells)
        {
            int count = 0;
            for (int i = 0; i < footprintCells.Count; i++)
            {
                Vector3Int c = footprintCells[i];
                var cellRect = new Rect(c.x, c.y, 1f, 1f);
                if (spriteRect.Overlaps(cellRect, allowInverse: true))
                    count++;
            }

            return count;
        }

        public static int CountCellOverlap(Bounds spriteBounds, IReadOnlyList<Vector3Int> footprintCells) =>
            CountCellOverlap(
                new Rect(spriteBounds.min.x, spriteBounds.min.y, spriteBounds.size.x, spriteBounds.size.y),
                footprintCells);

        public static int CountCellOverlap(
            Vector3 rootWorldPosition,
            Vector2 childLocalPosition,
            Vector2 childLocalScale,
            Vector2 spritePivot01,
            IReadOnlyList<Vector3Int> footprintCells,
            float unitSize = DefaultSpriteUnitSize)
        {
            Vector3 pivotWorld = rootWorldPosition + new Vector3(childLocalPosition.x, childLocalPosition.y, 0f);
            Rect rect = GetSpriteWorldRect(pivotWorld, spritePivot01, childLocalScale, unitSize);
            return CountCellOverlap(rect, footprintCells);
        }

        public static Vector2 GetSpritePivot01(Sprite sprite)
        {
            if (sprite == null)
                return BottomLeftPivot;

            return new Vector2(sprite.pivot.x / sprite.rect.width, sprite.pivot.y / sprite.rect.height);
        }
    }
}
