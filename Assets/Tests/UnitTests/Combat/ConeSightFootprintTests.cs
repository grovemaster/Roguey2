using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Actors.Components;
using JRogue.Controller.Enemy;
using JRogue.Core.Actor;
using JRogue.Manager.Map;
using JRogue.Tests.UnitTests.Input;
using NUnit.Framework;
using Roguey2.Sensing;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.Tests.UnitTests.Combat
{
    [TestFixture]
    public class ConeSightFootprintTests
    {
        readonly List<GameObject> _created = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            InputTestSceneBuilder.ResetSingletonManagersForTests();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _created.Count - 1; i >= 0; i--)
            {
                if (_created[i] != null)
                    Object.DestroyImmediate(_created[i]);
            }

            _created.Clear();
            InputTestSceneBuilder.ResetSingletonManagersForTests();
        }

        [Test]
        public void GetSightOriginCells_2x2_ReturnsFourOccupiedCells()
        {
            EnemyController enemy = CreateEnemy2x2(new Vector3Int(0, 0, 0));
            var origins = new List<Vector3Int>();

            ConeSightUtility.GetSightOriginCells(enemy, origins);

            Assert.AreEqual(4, origins.Count);
            Assert.IsTrue(origins.Contains(new Vector3Int(0, 0, 0)));
            Assert.IsTrue(origins.Contains(new Vector3Int(1, 0, 0)));
            Assert.IsTrue(origins.Contains(new Vector3Int(0, 1, 0)));
            Assert.IsTrue(origins.Contains(new Vector3Int(1, 1, 0)));
        }

        [Test]
        public void GetSightOriginCells_1x1_ReturnsAnchorOnly()
        {
            var go = new GameObject("SingleTileEnemy");
            _created.Add(go);
            var enemy = go.AddComponent<EnemyController>();
            go.AddComponent<GridMover>();
            var origins = new List<Vector3Int>();

            ConeSightUtility.GetSightOriginCells(enemy, origins);

            Assert.AreEqual(1, origins.Count);
            Assert.AreEqual(enemy.GridPosition, origins[0]);
        }

        [Test]
        public void GetNearestOriginCell_TargetAboveTopRow_PicksTopRowCell()
        {
            var origins = new List<Vector3Int>
            {
                new Vector3Int(0, 0, 0),
                new Vector3Int(1, 0, 0),
                new Vector3Int(0, 1, 0),
                new Vector3Int(1, 1, 0),
            };

            Vector3Int nearest = ConeSightUtility.GetNearestOriginCell(origins, new Vector3Int(1, 4, 0));

            Assert.AreEqual(new Vector3Int(1, 1, 0), nearest);
        }

        [Test]
        public void CollectVisibleTiles_2x2OpenFloor_IncludesTileNorthOfTopRow()
        {
            MapManager map = CreateOpenFloorMap();
            EnemyController enemy = CreateEnemy2x2(new Vector3Int(0, 0, 0));
            ConfigureSight(enemy, visionRange: 2, primaryConeAngle: 180f, facing: FacingDirection.North);

            var visible = new HashSet<Vector3Int>();
            ConeSightUtility.CollectVisibleTiles(
                enemy,
                map,
                enemy.VisionRange,
                enemy.PrimaryConeAngle,
                enemy.PeripheralRangeMultiplier,
                visible);

            Assert.IsTrue(
                visible.Contains(new Vector3Int(2, 1, 0)),
                "Multi-origin LOS should see past the anchor corner within range; anchor-only zone math would exclude this tile.");
        }

        [Test]
        public void TrySenseTarget_2x2OpenFloor_SeesTargetNorthOfTopRow()
        {
            MapManager map = CreateOpenFloorMap();
            EnemyController enemy = CreateEnemy2x2(new Vector3Int(0, 0, 0));
            ConfigureSight(enemy, visionRange: 2, primaryConeAngle: 180f, facing: FacingDirection.North);

            bool seen = ConeSightUtility.TrySenseTarget(
                enemy,
                new Vector3Int(2, 1, 0),
                map,
                enemy.VisionRange,
                enemy.PrimaryConeAngle,
                enemy.PeripheralRangeMultiplier,
                out ConeVisionZone zone);

            Assert.IsTrue(seen);
            Assert.AreNotEqual(ConeVisionZone.None, zone);
        }

        static void ConfigureSight(EnemyController enemy, int visionRange, float primaryConeAngle, FacingDirection facing)
        {
            InputTestSceneBuilder.SetPrivateField(enemy, "visionRange", visionRange);
            InputTestSceneBuilder.SetPrivateField(enemy, "primaryConeAngle", primaryConeAngle);
            enemy.currentFacing = facing;
        }

        EnemyController CreateEnemy2x2(Vector3Int anchor)
        {
            var go = new GameObject("GiantFootprintSight_Test");
            _created.Add(go);
            var enemy = go.AddComponent<EnemyController>();
            enemy.footprintWidth = 2;
            enemy.footprintHeight = 2;
            var mover = go.AddComponent<GridMover>();
            mover.SetGridPosition(anchor);
            return enemy;
        }

        MapManager CreateOpenFloorMap()
        {
            var root = new GameObject("OpenFloorMapRoot");
            _created.Add(root);

            var floorGo = new GameObject("Floor");
            floorGo.transform.SetParent(root.transform);
            _created.Add(floorGo);
            var floor = floorGo.AddComponent<Tilemap>();
            floorGo.AddComponent<TilemapRenderer>();

            Tile tile = ScriptableObject.CreateInstance<Tile>();

            for (int x = -2; x <= 4; x++)
            for (int y = -2; y <= 4; y++)
                floor.SetTile(new Vector3Int(x, y, 0), tile);

            var map = root.AddComponent<MapManager>();
            InputTestSceneBuilder.SetPrivateField(map, "floorMap", floor);
            InputTestSceneBuilder.SetPrivateField(map, "wallMap", CreateEmptyWallTilemap(root));

            return map;
        }

        Tilemap CreateEmptyWallTilemap(GameObject root)
        {
            var wallGo = new GameObject("Wall");
            wallGo.transform.SetParent(root.transform);
            _created.Add(wallGo);
            return wallGo.AddComponent<Tilemap>();
        }
    }
}
