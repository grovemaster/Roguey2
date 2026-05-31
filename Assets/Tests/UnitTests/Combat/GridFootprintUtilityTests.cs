using System.Collections.Generic;
using JRogue.Actors.Components;
using JRogue.Core.Actor;
using JRogue.Controller.Enemy;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.Combat
{
    [TestFixture]
    public class GridFootprintUtilityTests
    {
        [Test]
        public void Rectangle2x2_OccupiesFourCells()
        {
            var cells = new List<Vector3Int>();
            GridFootprintUtility.GetOccupiedCells(
                new Vector3Int(5, 5, 0),
                FootprintLayout.Rectangle,
                2,
                2,
                FacingDirection.North,
                cells);

            Assert.AreEqual(4, cells.Count);
            Assert.IsTrue(cells.Contains(new Vector3Int(5, 5, 0)));
            Assert.IsTrue(cells.Contains(new Vector3Int(6, 5, 0)));
            Assert.IsTrue(cells.Contains(new Vector3Int(5, 6, 0)));
            Assert.IsTrue(cells.Contains(new Vector3Int(6, 6, 0)));
        }

        [Test]
        public void SnakeEast_OccupiesThreeCellsAlongX()
        {
            var cells = new List<Vector3Int>();
            GridFootprintUtility.GetOccupiedCells(
                new Vector3Int(0, 0, 0),
                FootprintLayout.SnakeHeadBody,
                1,
                3,
                FacingDirection.East,
                cells);

            Assert.AreEqual(3, cells.Count);
            Assert.IsTrue(cells.Contains(new Vector3Int(0, 0, 0)));
            Assert.IsTrue(cells.Contains(new Vector3Int(1, 0, 0)));
            Assert.IsTrue(cells.Contains(new Vector3Int(2, 0, 0)));
        }

        [Test]
        public void DiagonalCorner_IsNotManhattanAdjacent_ButInMeleeBand()
        {
            var enemy = new GameObject("FootprintProbe").AddComponent<EnemyController>();
            enemy.footprintWidth = 2;
            enemy.footprintHeight = 2;
            enemy.SetGridPosition(new Vector3Int(10, 10, 0));

            Vector3Int corner = new Vector3Int(9, 9, 0);
            Assert.IsFalse(GridFootprintUtility.IsManhattanAdjacentToFootprint(corner, enemy));
            Assert.IsTrue(GridFootprintUtility.IsDiagonalCornerAdjacent(corner, enemy));
            Assert.IsTrue(GridFootprintUtility.CanMeleeTargetFootprint(corner, enemy));

            Object.DestroyImmediate(enemy.gameObject);
        }

        [Test]
        public void GetSingleCellActorWorldPosition_UsesCellCenter()
        {
            var cell = new Vector3Int(16, 10, 0);
            Vector3 cellCenter = new Vector3(16.5f, 10.5f, 0f);
            Vector3 world = GridFootprintUtility.GetSingleCellActorWorldPosition(cell, cellCenter, Vector3.one);

            Assert.AreEqual(16.5f, world.x, 0.001f);
            Assert.AreEqual(10.5f, world.y, 0.001f);
            Assert.AreEqual(cell, GridFootprintUtility.ResolveSingleCellAnchor(world, null, DefaultWorldToCell, Vector3.one));

            Vector3Int fromSubtract = GridFootprintUtility.ResolveSingleCellAnchor(
                new Vector3(16.5f, 10.5f, 0f),
                null,
                DefaultWorldToCell,
                Vector3.one);
            Assert.AreEqual(cell, fromSubtract);
        }

        static Vector3Int DefaultWorldToCell(Vector3 world) =>
            new Vector3Int(Mathf.FloorToInt(world.x), Mathf.FloorToInt(world.y), 0);

        [Test]
        public void CenterPivotAtAnchorWithoutOffset_OnlyOneCellOverlapsFootprint()
        {
            var anchor = new Vector3Int(-2, -2, 0);
            var cells = new List<Vector3Int>();
            GridFootprintUtility.GetOccupiedCells(anchor, FootprintLayout.Rectangle, 2, 2, FacingDirection.North, cells);

            Vector3 anchorRoot = GridFootprintUtility.GetFootprintAnchorWorldPosition(anchor);

            int overlap = FootprintSpriteAlignment.CountCellOverlap(
                anchorRoot,
                Vector2.zero,
                new Vector2(2f, 2f),
                new Vector2(0.5f, 0.5f),
                cells);

            Assert.AreEqual(1, overlap, "Center-pivot sprite at anchor with zero child offset.");
        }

        [Test]
        public void CenterPivotAtAnchorWithPivotOffset_AllFourCellsOverlapFootprint()
        {
            var anchor = new Vector3Int(-2, -2, 0);
            var cells = new List<Vector3Int>();
            GridFootprintUtility.GetOccupiedCells(anchor, FootprintLayout.Rectangle, 2, 2, FacingDirection.North, cells);

            Vector3 anchorRoot = GridFootprintUtility.GetFootprintAnchorWorldPosition(anchor);

            int overlap = FootprintSpriteAlignment.CountCellOverlap(
                anchorRoot,
                new Vector2(1f, 1f),
                new Vector2(2f, 2f),
                new Vector2(0.5f, 0.5f),
                cells);

            Assert.AreEqual(4, overlap);
        }

        [Test]
        public void GetFootprintVisualLocalOffset_CenterPivotSprite_ReturnsHalfFootprintSize()
        {
            var texture = new Texture2D(32, 32);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);

            Vector3 offset = GridFootprintUtility.GetFootprintVisualLocalOffset(
                sprite,
                FootprintLayout.Rectangle,
                2,
                2);

            Assert.AreEqual(new Vector3(1f, 1f, 0f), offset);

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void SyncFootprintPose_2x2DroppedAtAnchor_CoversFourFootprintCells()
        {
            var go = new GameObject("FootprintAlign_Test");
            var enemy = go.AddComponent<EnemyController>();
            enemy.footprintWidth = 2;
            enemy.footprintHeight = 2;
            var mover = go.AddComponent<GridMover>();

            var visualGo = new GameObject(FootprintPoseUtility.VisualChildName);
            visualGo.transform.SetParent(go.transform, false);
            var spriteRenderer = visualGo.AddComponent<SpriteRenderer>();
            var texture = new Texture2D(32, 32);
            spriteRenderer.sprite = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);

            Vector3Int anchor = new Vector3Int(-2, -2, 0);
            go.transform.position = new Vector3(anchor.x, anchor.y, 0f);
            mover.SetGridPosition(anchor);
            mover.SyncFootprintPose();

            Assert.AreEqual(GridFootprintUtility.GetFootprintAnchorWorldPosition(anchor), go.transform.position);
            Assert.AreEqual(new Vector3(1f, 1f, 0f), visualGo.transform.localPosition);

            var cells = new List<Vector3Int>();
            enemy.GetOccupiedCells(cells);
            Assert.AreEqual(4, FootprintSpriteAlignment.CountCellOverlap(spriteRenderer.bounds, cells));

            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void ApplyVisual_2x2_CenterPivotSprite_OffsetsChildByHalfFootprint()
        {
            var root = new GameObject("FootprintRoot");
            var visualGo = new GameObject(FootprintPoseUtility.VisualChildName);
            visualGo.transform.SetParent(root.transform, false);
            var texture = new Texture2D(32, 32);
            visualGo.AddComponent<SpriteRenderer>().sprite =
                Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);

            FootprintPoseUtility.ApplyVisual(
                new Vector3Int(-2, -2, 0),
                FootprintLayout.Rectangle,
                2,
                2,
                FacingDirection.North,
                root.transform);

            Assert.AreEqual(new Vector3(1f, 1f, 0f), visualGo.transform.localPosition);
            Assert.AreEqual(new Vector3(2f, 2f, 1f), visualGo.transform.localScale);

            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void NorthOfTopRow_IsManhattanAdjacent_NotDiagonalCorner()
        {
            var enemy = new GameObject("FootprintProbe").AddComponent<EnemyController>();
            enemy.footprintWidth = 2;
            enemy.footprintHeight = 2;
            enemy.SetGridPosition(new Vector3Int(0, 0, 0));

            Vector3Int north = new Vector3Int(0, 2, 0);
            Assert.IsTrue(GridFootprintUtility.IsManhattanAdjacentToFootprint(north, enemy));
            Assert.IsFalse(GridFootprintUtility.IsDiagonalCornerAdjacent(north, enemy));

            Object.DestroyImmediate(enemy.gameObject);
        }

        sealed class FootprintProbe : IGridFootprint
        {
            public FootprintProbe(Vector3Int gridPosition, int width, int height)
            {
                GridPosition = gridPosition;
                Width = width;
                Height = height;
            }

            public Vector3Int GridPosition { get; }
            public int Width { get; }
            public int Height { get; }
            public FootprintLayout Layout => FootprintLayout.Rectangle;
            public int FootprintWidth => Width;
            public int FootprintHeight => Height;
            public FacingDirection Facing => FacingDirection.North;

            public void GetOccupiedCells(List<Vector3Int> buffer) =>
                GridFootprintUtility.GetOccupiedCells(this, buffer);

            public bool Occupies(Vector3Int cell) =>
                GridFootprintUtility.Occupies(this, cell);
        }
    }
}
