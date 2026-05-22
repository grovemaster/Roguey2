using System.Collections.Generic;
using UnityEngine;

namespace JRogue.Core.Actor
{
    public static class GridFootprintUtility
    {
        public static bool IsSingleCell(IGridFootprint footprint) =>
            footprint != null && IsSingleCell(footprint.Layout, footprint.FootprintWidth, footprint.FootprintHeight);

        public static bool IsSingleCell(FootprintLayout layout, int width, int height) =>
            layout == FootprintLayout.Rectangle && width <= 1 && height <= 1;

        /// <summary>
        /// Derives the footprint anchor from a scene transform position (integer corner or footprint center).
        /// </summary>
        public static Vector3Int ResolvePlacementAnchor(Vector3 worldPosition, IGridFootprint footprint)
        {
            if (footprint == null)
                return Vector3Int.FloorToInt(worldPosition - new Vector3(0.5f, 0.5f, 0f));

            if (IsSingleCell(footprint))
                return Vector3Int.FloorToInt(worldPosition - new Vector3(0.5f, 0.5f, 0f));

            if (IsAnchorCornerPlacement(worldPosition))
                return new Vector3Int(Mathf.RoundToInt(worldPosition.x), Mathf.RoundToInt(worldPosition.y), 0);

            if (footprint.Layout == FootprintLayout.Rectangle)
            {
                return new Vector3Int(
                    Mathf.RoundToInt(worldPosition.x - footprint.FootprintWidth * 0.5f),
                    Mathf.RoundToInt(worldPosition.y - footprint.FootprintHeight * 0.5f),
                    0);
            }

            Vector3Int step = SnakeStepOffset(footprint.Facing);
            if (step.x != 0)
            {
                float headX = step.x > 0 ? worldPosition.x - 1.5f : worldPosition.x - 0.5f;
                return new Vector3Int(Mathf.FloorToInt(headX), Mathf.FloorToInt(worldPosition.y - 0.5f), 0);
            }

            float headY = step.y < 0 ? worldPosition.y + 0.5f : worldPosition.y - 1.5f;
            return new Vector3Int(Mathf.FloorToInt(worldPosition.x - 0.5f), Mathf.FloorToInt(headY), 0);
        }

        static bool IsAnchorCornerPlacement(Vector3 worldPosition)
        {
            const float epsilon = 0.05f;
            float fracX = worldPosition.x - Mathf.Floor(worldPosition.x);
            float fracY = worldPosition.y - Mathf.Floor(worldPosition.y);
            return fracX < epsilon && fracY < epsilon;
        }

        public static void GetOccupiedCells(
            Vector3Int anchor,
            FootprintLayout layout,
            int width,
            int height,
            FacingDirection facing,
            List<Vector3Int> buffer)
        {
            buffer.Clear();
            width = Mathf.Max(1, width);
            height = Mathf.Max(1, height);

            if (layout == FootprintLayout.SnakeHeadBody)
            {
                buffer.Add(anchor);
                Vector3Int d = SnakeStepOffset(facing);
                buffer.Add(anchor + d);
                buffer.Add(anchor + d * 2);
                return;
            }

            for (int oy = 0; oy < height; oy++)
            for (int ox = 0; ox < width; ox++)
                buffer.Add(new Vector3Int(anchor.x + ox, anchor.y + oy, anchor.z));
        }

        public static void GetOccupiedCells(IGridFootprint footprint, List<Vector3Int> buffer)
        {
            if (footprint == null)
            {
                buffer.Clear();
                return;
            }

            GetOccupiedCells(
                footprint.GridPosition,
                footprint.Layout,
                footprint.FootprintWidth,
                footprint.FootprintHeight,
                footprint.Facing,
                buffer);
        }

        public static bool Occupies(
            Vector3Int anchor,
            FootprintLayout layout,
            int width,
            int height,
            FacingDirection facing,
            Vector3Int cell)
        {
            var scratch = new List<Vector3Int>(8);
            GetOccupiedCells(anchor, layout, width, height, facing, scratch);
            for (int i = 0; i < scratch.Count; i++)
            {
                if (scratch[i] == cell)
                    return true;
            }

            return false;
        }

        public static bool Occupies(IGridFootprint footprint, Vector3Int cell) =>
            footprint != null && Occupies(
                footprint.GridPosition,
                footprint.Layout,
                footprint.FootprintWidth,
                footprint.FootprintHeight,
                footprint.Facing,
                cell);

        public static Vector3 GetFootprintWorldCenter(IGridFootprint footprint)
        {
            if (footprint == null)
                return Vector3.zero;

            var cells = new List<Vector3Int>(8);
            GetOccupiedCells(footprint, cells);
            if (cells.Count == 0)
                return new Vector3(footprint.GridPosition.x + 0.5f, footprint.GridPosition.y + 0.5f, 0f);

            int minX = cells[0].x, maxX = cells[0].x, minY = cells[0].y, maxY = cells[0].y;
            for (int i = 1; i < cells.Count; i++)
            {
                minX = Mathf.Min(minX, cells[i].x);
                maxX = Mathf.Max(maxX, cells[i].x);
                minY = Mathf.Min(minY, cells[i].y);
                maxY = Mathf.Max(maxY, cells[i].y);
            }

            return new Vector3((minX + maxX + 1f) * 0.5f, (minY + maxY + 1f) * 0.5f, 0f);
        }

        /// <summary>
        /// World position for the footprint anchor (bottom-left cell corner). Multi-tile actors with
        /// bottom-left sprite pivots use this as the root transform position.
        /// </summary>
        public static Vector3 GetFootprintAnchorWorldPosition(Vector3Int anchor) =>
            new Vector3(anchor.x, anchor.y, 0f);

        public static Vector3 GetFootprintAnchorWorldPosition(IGridFootprint footprint) =>
            footprint == null
                ? Vector3.zero
                : GetFootprintAnchorWorldPosition(footprint.GridPosition);

        /// <summary>
        /// Local offset for <see cref="FootprintPoseUtility.VisualChildName"/> when the root sits on the
        /// footprint anchor. Places the sprite pivot so the scaled quad covers anchor..anchor+(width,height).
        /// </summary>
        public static Vector3 GetFootprintVisualLocalOffset(
            Sprite sprite,
            FootprintLayout layout,
            int width,
            int height)
        {
            if (sprite == null || IsSingleCell(layout, width, height))
                return Vector3.zero;

            float pivotX = sprite.pivot.x / sprite.rect.width;
            float pivotY = sprite.pivot.y / sprite.rect.height;
            return new Vector3(width * pivotX, height * pivotY, 0f);
        }

        public static Vector3 GetFootprintVisualLocalOffset(
            Vector3Int anchor,
            FootprintLayout layout,
            int width,
            int height,
            FacingDirection facing) =>
            Vector3.zero;

        public static Vector3 GetFootprintVisualLocalOffset(IGridFootprint footprint) =>
            footprint == null
                ? Vector3.zero
                : GetFootprintVisualLocalOffset(
                    footprint.GridPosition,
                    footprint.Layout,
                    footprint.FootprintWidth,
                    footprint.FootprintHeight,
                    footprint.Facing);

        public static Vector3 GetFootprintVisualLocalScale(
            FootprintLayout layout,
            int width,
            int height,
            FacingDirection facing)
        {
            if (layout == FootprintLayout.SnakeHeadBody)
            {
                return facing == FacingDirection.East || facing == FacingDirection.West
                    ? new Vector3(3f, 1f, 1f)
                    : new Vector3(1f, 3f, 1f);
            }

            return new Vector3(Mathf.Max(1, width), Mathf.Max(1, height), 1f);
        }

        public static int ManhattanDistanceToFootprint(Vector3Int from, IGridFootprint footprint)
        {
            if (footprint == null)
                return int.MaxValue;

            var cells = new List<Vector3Int>(8);
            GetOccupiedCells(footprint, cells);
            int best = int.MaxValue;
            for (int i = 0; i < cells.Count; i++)
            {
                int d = Mathf.Abs(from.x - cells[i].x) + Mathf.Abs(from.y - cells[i].y);
                if (d < best)
                    best = d;
            }

            return best;
        }

        public static bool IsManhattanAdjacentToFootprint(Vector3Int from, IGridFootprint footprint) =>
            footprint != null && ManhattanDistanceToFootprint(from, footprint) <= 1;

        /// <summary>
        /// Diagonal-corner band around the footprint AABB (Chebyshev-adjacent but not Manhattan-adjacent to any occupied cell).
        /// </summary>
        public static bool IsDiagonalCornerAdjacent(Vector3Int from, IGridFootprint footprint)
        {
            if (footprint == null || IsSingleCell(footprint))
                return false;

            if (IsManhattanAdjacentToFootprint(from, footprint))
                return false;

            var cells = new List<Vector3Int>(8);
            GetOccupiedCells(footprint, cells);
            int minX = cells[0].x, maxX = cells[0].x, minY = cells[0].y, maxY = cells[0].y;
            for (int i = 1; i < cells.Count; i++)
            {
                minX = Mathf.Min(minX, cells[i].x);
                maxX = Mathf.Max(maxX, cells[i].x);
                minY = Mathf.Min(minY, cells[i].y);
                maxY = Mathf.Max(maxY, cells[i].y);
            }

            int cheb = Mathf.Max(
                Mathf.Max(minX - from.x, from.x - maxX),
                Mathf.Max(minY - from.y, from.y - maxY));
            return cheb <= 1;
        }

        public static bool CanMeleeTargetFootprint(Vector3Int attackerCell, IGridFootprint defender) =>
            defender != null
            && (IsManhattanAdjacentToFootprint(attackerCell, defender)
                || IsDiagonalCornerAdjacent(attackerCell, defender));

        public static void GetDiagonalCornerCells(IGridFootprint footprint, List<Vector3Int> buffer)
        {
            buffer.Clear();
            if (footprint == null || IsSingleCell(footprint))
                return;

            var cells = new List<Vector3Int>(8);
            GetOccupiedCells(footprint, cells);
            int minX = cells[0].x, maxX = cells[0].x, minY = cells[0].y, maxY = cells[0].y;
            for (int i = 1; i < cells.Count; i++)
            {
                minX = Mathf.Min(minX, cells[i].x);
                maxX = Mathf.Max(maxX, cells[i].x);
                minY = Mathf.Min(minY, cells[i].y);
                maxY = Mathf.Max(maxY, cells[i].y);
            }

            int z = footprint.GridPosition.z;
            buffer.Add(new Vector3Int(minX - 1, maxY + 1, z));
            buffer.Add(new Vector3Int(maxX + 1, maxY + 1, z));
            buffer.Add(new Vector3Int(maxX + 1, minY - 1, z));
            buffer.Add(new Vector3Int(minX - 1, minY - 1, z));
        }

        public static Vector3Int SnakeStepOffset(FacingDirection facing)
        {
            switch (facing)
            {
                case FacingDirection.North: return new Vector3Int(0, -1, 0);
                case FacingDirection.South: return new Vector3Int(0, 1, 0);
                case FacingDirection.East: return new Vector3Int(1, 0, 0);
                case FacingDirection.West: return new Vector3Int(-1, 0, 0);
                default: return new Vector3Int(1, 0, 0);
            }
        }
    }

    /// <summary>
    /// Aligns a multi-tile actor root (footprint anchor / bottom-left) and optional <see cref="VisualChildName"/> child
    /// (bottom-left pivot sprite scaled to footprint size) to the same grid anchor in editor and play mode.
    /// </summary>
    public static class FootprintPoseUtility
    {
        public const string VisualChildName = "FootprintVisual";

        public static Transform FindVisual(Transform root) =>
            root != null ? root.Find(VisualChildName) : null;

        public static void ApplyVisual(IGridFootprint footprint, Transform root)
        {
            if (footprint == null || root == null)
                return;

            ApplyVisual(
                footprint.GridPosition,
                footprint.Layout,
                footprint.FootprintWidth,
                footprint.FootprintHeight,
                footprint.Facing,
                root);
        }

        public static void ApplyVisual(
            Vector3Int anchor,
            FootprintLayout layout,
            int width,
            int height,
            FacingDirection facing,
            Transform root)
        {
            if (root == null)
                return;

            root.localScale = Vector3.one;
            Transform visual = FindVisual(root);
            if (visual == null)
                return;

            if (GridFootprintUtility.IsSingleCell(layout, width, height))
            {
                visual.localPosition = Vector3.zero;
                visual.localScale = Vector3.one;
                return;
            }

            SpriteRenderer spriteRenderer = visual.GetComponent<SpriteRenderer>();
            visual.localPosition = GridFootprintUtility.GetFootprintVisualLocalOffset(
                spriteRenderer != null ? spriteRenderer.sprite : null,
                layout,
                width,
                height);
            visual.localScale = GridFootprintUtility.GetFootprintVisualLocalScale(layout, width, height, facing);
        }

        public static Vector3 GetRootWorldPosition(
            Vector3Int anchor,
            FootprintLayout layout,
            int width,
            int height,
            FacingDirection facing) =>
            GridFootprintUtility.IsSingleCell(layout, width, height)
                ? new Vector3(anchor.x + 0.5f, anchor.y + 0.5f, 0f)
                : GridFootprintUtility.GetFootprintAnchorWorldPosition(anchor);
    }
}
