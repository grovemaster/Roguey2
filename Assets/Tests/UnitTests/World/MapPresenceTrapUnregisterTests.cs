using JRogue.Manager.Map;
using JRogue.Traps;
using JRogue.World.MapPresence;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.Tests.World
{
  public class MapPresenceTrapUnregisterTests
  {
    GameObject _mapGo;
    GameObject _trapGo;
    MapManager _map;
    TrapService _traps;

    [SetUp]
    public void SetUp()
    {
      _mapGo = new GameObject("Map");
      _map = _mapGo.AddComponent<MapManager>();

      var floorGo = new GameObject("Floor");
      floorGo.transform.SetParent(_mapGo.transform);
      var floor = floorGo.AddComponent<Tilemap>();
      SetPrivateField(_map, "floorMap", floor);

      var wallGo = new GameObject("Wall");
      wallGo.transform.SetParent(_mapGo.transform);
      var wall = wallGo.AddComponent<Tilemap>();
      SetPrivateField(_map, "wallMap", wall);

      var cell = new Vector3Int(-1, -1, 0);
      floor.SetTile(cell, ScriptableObject.CreateInstance<Tile>());

      _trapGo = new GameObject("Traps");
      _traps = _trapGo.AddComponent<TrapService>();
    }

    [TearDown]
    public void TearDown()
    {
      Object.DestroyImmediate(_trapGo);
      Object.DestroyImmediate(_mapGo);
    }

    [Test]
    public void TryUnregisterFloorTrap_RemovesTrapFromCell()
    {
      var def = ScriptableObject.CreateInstance<TrapDefinition>();
      def.trapId = TrapId.Bear;
      def.placement = TrapPlacement.Floor;
      def.initialVisibility = TrapVisibility.Visible;

      var cell = new Vector3Int(-1, -1, 0);
      _traps.Register(cell, def);
      Assert.IsTrue(_traps.IsFloorTrapAt(cell));

      Assert.IsTrue(_traps.TryUnregisterFloorTrap(cell));
      Assert.IsFalse(_traps.IsFloorTrapAt(cell));
    }

    [Test]
    public void TrapWhileAliveEffect_RegisterAndRevert_ClearsTrap()
    {
      var def = ScriptableObject.CreateInstance<TrapDefinition>();
      def.trapId = TrapId.Bear;
      def.placement = TrapPlacement.Floor;
      def.initialVisibility = TrapVisibility.Visible;

      var effect = ScriptableObject.CreateInstance<TrapWhileAliveMapEffect>();
      effect.cell = new Vector3Int(-1, -1, 0);
      effect.trapDefinition = def;

      var profile = ScriptableObject.CreateInstance<MonsterMapPresenceProfile>();
      profile.effects = new MonsterMapPresenceEffect[] { effect };

      var ctx = new MonsterMapPresenceContext(null, profile);
      effect.Apply(ctx);
      Assert.IsTrue(_traps.IsFloorTrapAt(effect.cell));

      ctx.RevertAll();
      Assert.IsFalse(_traps.IsFloorTrapAt(effect.cell));
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
