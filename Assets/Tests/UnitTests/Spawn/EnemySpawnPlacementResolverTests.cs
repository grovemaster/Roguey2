using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Actors.Components;
using JRogue.Controller.Enemy;
using JRogue.Core.Actor;
using JRogue.Interactables;
using JRogue.Manager.Grid;
using JRogue.Manager.Map;
using JRogue.Spawn;
using JRogue.Stats;
using JRogue.Tests.UnitTests.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.Tests.UnitTests.Spawn
{
    [TestFixture]
    public sealed class EnemySpawnPlacementResolverTests
    {
        readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _created)
            {
                if (go != null)
                    Object.DestroyImmediate(go);
            }

            _created.Clear();
            InputTestSceneBuilder.ResetSingletonManagersForTests();
        }

        [Test]
        public void NorthOfOrigin_PreferredWhenClear()
        {
            var ctx = CreateContext(radius: 4);
            Vector3Int origin = new Vector3Int(0, 0, 0);

            Assert.IsTrue(EnemySpawnPlacementResolver.TryResolveAnchor(
                origin,
                EnemySpawnPlacementPolicy.NorthOfOriginThenNearestUnoccupiedFloor,
                new Vector3Int(0, 1, 0),
                FootprintLayout.Rectangle,
                1,
                1,
                FacingDirection.North,
                ctx.Map,
                ctx.Grid,
                ctx.Interactables,
                out Vector3Int anchor));

            Assert.AreEqual(new Vector3Int(0, 1, 0), anchor);
        }

        [Test]
        public void NorthBlocked_FallsBackToNearestFloor()
        {
            var ctx = CreateContext(radius: 4);
            Vector3Int origin = new Vector3Int(0, 0, 0);
            ctx.Map.WallMap.SetTile(new Vector3Int(0, 1, 0), ctx.WallTile);

            Assert.IsTrue(EnemySpawnPlacementResolver.TryResolveAnchor(
                origin,
                EnemySpawnPlacementPolicy.NorthOfOriginThenNearestUnoccupiedFloor,
                new Vector3Int(0, 1, 0),
                FootprintLayout.Rectangle,
                1,
                1,
                FacingDirection.North,
                ctx.Map,
                ctx.Grid,
                ctx.Interactables,
                out Vector3Int anchor));

            Assert.AreNotEqual(new Vector3Int(0, 1, 0), anchor);
            Assert.IsTrue(EnemySpawnPlacementResolver.CanPlaceFootprintAt(
                anchor,
                FootprintLayout.Rectangle,
                1,
                1,
                FacingDirection.North,
                ctx.Map,
                ctx.Grid,
                ctx.Interactables));
        }

        [Test]
        public void OccupiedNorth_PicksAnotherTile()
        {
            var ctx = CreateContext(radius: 4);
            Vector3Int origin = new Vector3Int(0, 0, 0);
            Vector3Int north = new Vector3Int(0, 1, 0);

            GameObject blocker = new GameObject("Blocker");
            _created.Add(blocker);
            blocker.AddComponent<CharacterStats>();
            blocker.AddComponent<HealthComponent>();
            var mover = blocker.AddComponent<GridMover>();
            mover.InitializeAtGridAnchor(north);

            Assert.IsTrue(EnemySpawnPlacementResolver.TryResolveAnchor(
                origin,
                EnemySpawnPlacementPolicy.NorthOfOriginThenNearestUnoccupiedFloor,
                new Vector3Int(0, 1, 0),
                FootprintLayout.Rectangle,
                1,
                1,
                FacingDirection.North,
                ctx.Map,
                ctx.Grid,
                ctx.Interactables,
                out Vector3Int anchor));

            Assert.AreNotEqual(north, anchor);
        }

        TestContext CreateContext(int radius)
        {
            InputTestSceneBuilder.ResetSingletonManagersForTests();

            var root = new GameObject("SpawnTest_Map");
            _created.Add(root);

            var gridObj = new GameObject("Grid");
            gridObj.transform.SetParent(root.transform);
            _created.Add(gridObj);
            var grid = gridObj.AddComponent<Grid>();
            grid.cellSize = Vector3.one;

            var floorObj = new GameObject("Floor");
            floorObj.transform.SetParent(gridObj.transform);
            var floor = floorObj.AddComponent<Tilemap>();
            floorObj.AddComponent<TilemapRenderer>();
            var tile = ScriptableObject.CreateInstance<Tile>();
            for (int x = -radius; x <= radius; x++)
            for (int y = -radius; y <= radius; y++)
                floor.SetTile(new Vector3Int(x, y, 0), tile);

            var wallObj = new GameObject("Wall");
            wallObj.transform.SetParent(gridObj.transform);
            var wall = wallObj.AddComponent<Tilemap>();
            wallObj.AddComponent<TilemapRenderer>();

            var map = root.AddComponent<MapManager>();
            InputTestSceneBuilder.SetPrivateField(map, "floorMap", floor);
            InputTestSceneBuilder.SetPrivateField(map, "wallMap", wall);

            var gridMgrObj = new GameObject("GridManager");
            _created.Add(gridMgrObj);
            var gridMgr = gridMgrObj.AddComponent<GridManager>();

            var interactableObj = new GameObject("Interactables");
            _created.Add(interactableObj);
            var interactables = interactableObj.AddComponent<InteractableTileService>();

            return new TestContext(map, wall, tile, gridMgr, interactables);
        }

        readonly struct TestContext
        {
            public TestContext(
                MapManager map,
                Tilemap wallMap,
                Tile wallTile,
                GridManager grid,
                InteractableTileService interactables)
            {
                Map = map;
                WallMap = wallMap;
                WallTile = wallTile;
                Grid = grid;
                Interactables = interactables;
            }

            public MapManager Map { get; }
            public Tilemap WallMap { get; }
            public Tile WallTile { get; }
            public GridManager Grid { get; }
            public InteractableTileService Interactables { get; }
        }
    }
}
