using JRogue.Data.Door;
using JRogue.Manager.Door;
using JRogue.Manager.Map;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.Tests.Doors
{
    public class DoorServiceTests
    {
        GameObject _doorGo;
        GameObject _mapGo;
        DoorService _doors;
        MapManager _map;

        [SetUp]
        public void SetUp()
        {
            _mapGo = new GameObject("Map");
            _map = _mapGo.AddComponent<MapManager>();

            var floorGo = new GameObject("Floor");
            floorGo.transform.SetParent(_mapGo.transform);
            var floor = floorGo.AddComponent<Tilemap>();
            _mapGo.GetComponent<MapManager>();
            SetPrivateField(_map, "floorMap", floor);

            var wallGo = new GameObject("Wall");
            wallGo.transform.SetParent(_mapGo.transform);
            var wall = wallGo.AddComponent<Tilemap>();
            SetPrivateField(_map, "wallMap", wall);

            var doorCell = new Vector3Int(2, 0, 0);
            floor.SetTile(doorCell, ScriptableObject.CreateInstance<Tile>());

            _doorGo = new GameObject("Doors");
            _doors = _doorGo.AddComponent<DoorService>();

            var def = ScriptableObject.CreateInstance<DoorDefinition>();
            def.doorId = "TestDoor";
            def.orientation = DoorOrientation.Horizontal;

            _doors.Register(new DoorPlacement
            {
                definition = def,
                cell = doorCell,
                overrideLocked = true,
                startsLocked = true,
            });
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_doorGo);
            Object.DestroyImmediate(_mapGo);
        }

        [Test]
        public void ClosedDoor_BlocksMovement()
        {
            Assert.IsTrue(_doors.BlocksMovement(new Vector3Int(2, 0, 0)));
            Assert.IsFalse(_map.IsWalkable(new Vector3Int(2, 0, 0)));
        }

        [Test]
        public void UnlockAndOpen_BecomesWalkable()
        {
            _doors.Unlock("TestDoor");
            Assert.IsTrue(_doors.TryOpen("TestDoor"));
            Assert.IsFalse(_doors.BlocksMovement(new Vector3Int(2, 0, 0)));
            Assert.IsTrue(_map.IsWalkable(new Vector3Int(2, 0, 0)));
        }

        [Test]
        public void Break_BecomesWalkable()
        {
            Assert.IsTrue(_doors.TryGetById("TestDoor", out DoorInstance door));
            Assert.IsTrue(_doors.TryBreak(door));
            Assert.IsTrue(_map.IsWalkable(door.Cell));
        }

        static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(
                fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field?.SetValue(target, value);
        }
    }
}
