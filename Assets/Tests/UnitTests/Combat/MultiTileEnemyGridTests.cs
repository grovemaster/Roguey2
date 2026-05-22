using System.Collections.Generic;
using System.Text.RegularExpressions;
using JRogue.Actors;
using JRogue.Actors.Components;
using JRogue.Controller.Enemy;
using JRogue.Core.Actor;
using JRogue.Manager.Grid;
using JRogue.Manager.Map;
using JRogue.Tests.UnitTests.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

namespace JRogue.Tests.UnitTests.Combat
{
    [TestFixture]
    public class MultiTileEnemyGridTests
    {
        readonly List<GameObject> _created = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            InputTestSceneBuilder.ResetSingletonManagersForTests();
            LogAssert.ignoreFailingMessages = true;
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            for (int i = _created.Count - 1; i >= 0; i--)
            {
                if (_created[i] != null)
                    Object.DestroyImmediate(_created[i]);
            }

            _created.Clear();
            InputTestSceneBuilder.ResetSingletonManagersForTests();
        }

        [Test]
        public void RegisterFootprint_2x2_AllCellsResolveToOwner()
        {
            GridManager grid = CreateGridManager();
            EnemyController enemy = CreateEnemy2x2(new Vector3Int(4, 4, 0));
            var cells = new List<Vector3Int>();
            enemy.GetOccupiedCells(cells);
            Assert.IsTrue(grid.TryRegisterFootprint(enemy, cells));

            Assert.AreSame(enemy, grid.GetActorAt(new Vector3Int(4, 4, 0)));
            Assert.AreSame(enemy, grid.GetActorAt(new Vector3Int(5, 4, 0)));
            Assert.AreSame(enemy, grid.GetActorAt(new Vector3Int(4, 5, 0)));
            Assert.AreSame(enemy, grid.GetActorAt(new Vector3Int(5, 5, 0)));
        }

        [Test]
        public void GetAllActors_FootprintFourCells_ReturnsOneEntry()
        {
            GridManager grid = CreateGridManager();
            EnemyController enemy = CreateEnemy2x2(new Vector3Int(0, 0, 0));
            var cells = new List<Vector3Int>();
            enemy.GetOccupiedCells(cells);
            grid.TryRegisterFootprint(enemy, cells);

            int count = 0;
            foreach (IBattleTarget _ in grid.GetAllActors())
                count++;

            Assert.AreEqual(1, count);
        }

        [Test]
        public void TryMove_2x2Into1WideGap_Fails()
        {
            CreateCorridorMap();
            GridManager grid = CreateGridManager();
            EnemyController enemy = CreateEnemy2x2(new Vector3Int(2, 2, 0));
            RegisterEnemyFootprint(grid, enemy);

            Assert.IsFalse(enemy.TryMove(Vector3Int.right));
            Assert.AreEqual(new Vector3Int(2, 2, 0), enemy.GridPosition);
        }

        [Test]
        public void SnakeEast_MoveEast_ShiftsAllThreeCells()
        {
            CreateOpenFloor(12);
            GridManager grid = CreateGridManager();
            EnemyController snake = CreateSnake(FacingDirection.East, new Vector3Int(3, 3, 0));
            RegisterEnemyFootprint(grid, snake);

            Assert.IsTrue(snake.TryMove(Vector3Int.right));
            Assert.AreEqual(new Vector3Int(4, 3, 0), snake.GridPosition);
            Assert.IsTrue(snake.Occupies(new Vector3Int(4, 3, 0)));
            Assert.IsTrue(snake.Occupies(new Vector3Int(5, 3, 0)));
            Assert.IsTrue(snake.Occupies(new Vector3Int(6, 3, 0)));
        }

        static void RegisterEnemyFootprint(GridManager grid, EnemyController enemy)
        {
            var cells = new List<Vector3Int>();
            enemy.GetOccupiedCells(cells);
            grid.TryRegisterFootprint(enemy, cells);
        }

        GridManager CreateGridManager()
        {
            var go = new GameObject("GridManager_Test");
            _created.Add(go);
            return go.AddComponent<GridManager>();
        }

        MapManager CreateOpenFloor(int radius)
        {
            var root = new GameObject("Map_Test");
            _created.Add(root);
            var tilemapGo = new GameObject("Floor");
            tilemapGo.transform.SetParent(root.transform);
            _created.Add(tilemapGo);
            var tilemap = tilemapGo.AddComponent<Tilemap>();
            var tile = ScriptableObject.CreateInstance<Tile>();
            for (int x = -radius; x <= radius; x++)
            for (int y = -radius; y <= radius; y++)
                tilemap.SetTile(new Vector3Int(x, y, 0), tile);

            var map = root.AddComponent<MapManager>();
            InputTestSceneBuilder.SetPrivateField(map, "floorMap", tilemap);
            InputTestSceneBuilder.SetPrivateField(map, "wallMap", CreateEmptyWallTilemap(root));
            return map;
        }

        MapManager CreateCorridorMap()
        {
            var root = new GameObject("Map_Corridor");
            _created.Add(root);
            var tilemapGo = new GameObject("Floor");
            tilemapGo.transform.SetParent(root.transform);
            _created.Add(tilemapGo);
            var tilemap = tilemapGo.AddComponent<Tilemap>();
            var tile = ScriptableObject.CreateInstance<Tile>();
            for (int y = 0; y < 8; y++)
                tilemap.SetTile(new Vector3Int(2, y, 0), tile);
            for (int x = 3; x <= 8; x++)
                tilemap.SetTile(new Vector3Int(x, 2, 0), tile);

            var map = root.AddComponent<MapManager>();
            InputTestSceneBuilder.SetPrivateField(map, "floorMap", tilemap);
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

        static EnemyController CreateEnemy2x2(Vector3Int anchor)
        {
            var go = new GameObject("GiantSkeleton_Test");
            var enemy = go.AddComponent<EnemyController>();
            enemy.footprintWidth = 2;
            enemy.footprintHeight = 2;
            enemy.SetGridPosition(anchor);
            go.AddComponent<GridMover>();
            return enemy;
        }

        static EnemyController CreateSnake(FacingDirection facing, Vector3Int anchor)
        {
            var go = new GameObject("Snake_Test");
            var enemy = go.AddComponent<EnemyController>();
            enemy.footprintLayout = FootprintLayout.SnakeHeadBody;
            enemy.currentFacing = facing;
            enemy.SetGridPosition(anchor);
            go.AddComponent<GridMover>();
            return enemy;
        }
    }
}
