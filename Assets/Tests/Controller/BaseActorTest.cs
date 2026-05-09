using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using JRogue.Actors;
using JRogue.Actors.Components;
using JRogue.Core.Actor;
using JRogue.Manager.Grid;
using JRogue.Manager.Map;
using JRogue.Manager.Party;
using JRogue.Manager.Turn;
using JRogue.Tests.UnitTests.Input;
using JRogue.Tests.UnitTests.MockMonoBehavior;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

namespace JRogue.Tests.Controller
{
    [TestFixture]
    public class BaseActorTest
    {
        private readonly List<GameObject> _createdObjects = new List<GameObject>();

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
            foreach (GameObject go in _createdObjects)
            {
                if (go != null)
                    Object.DestroyImmediate(go);
            }

            _createdObjects.Clear();
            InputTestSceneBuilder.ResetSingletonManagersForTests();
        }

        [Test]
        public void SetGridPosition_GetGridPosition_AndGridProperty_Agree()
        {
            TestActor actor = CreateActorOnWalkableGrid(Vector3Int.zero);
            Vector3Int p = new Vector3Int(3, -2, 0);
            actor.SetGridPosition(p);
            Assert.AreEqual(p, actor.GetGridPosition());
            Assert.AreEqual(p, actor.GridPosition);
        }

        [Test]
        public void Owner_ReturnsActorsGameObject()
        {
            TestActor actor = CreateActorOnWalkableGrid(Vector3Int.zero);
            Assert.AreSame(actor.gameObject, actor.Owner);
        }

        [Test]
        public void SyncPosition_SetsTransformToCellCenter()
        {
            TestActor actor = CreateActorOnWalkableGrid(new Vector3Int(2, 5, 0));
            actor.SyncPosition();
            Assert.AreEqual(new Vector3(2.5f, 5.5f, 0f), actor.transform.position);
        }

        [Test]
        public void TryMove_TargetNotWalkable_ReturnsFalse_PositionUnchanged()
        {
            MapManager map = CreateMapWithFloorRadius(2);
            TestActor actor = CreateActor(map, new Vector3Int(0, 0, 0));
            RegisterActor(actor, new Vector3Int(0, 0, 0));

            Vector3Int before = actor.GridPosition;
            Assert.IsFalse(actor.TryMove(Vector3Int.right * 5));
            Assert.AreEqual(before, actor.GridPosition);
        }

        [Test]
        public void TryMove_EmptyWalkableTile_UpdatesPositionAndGridRegistration()
        {
            MapManager map = CreateMapWithFloorRadius(4);
            TestActor actor = CreateActor(map, new Vector3Int(0, 0, 0));
            RegisterActor(actor, new Vector3Int(0, 0, 0));

            Assert.IsTrue(actor.TryMove(Vector3Int.right));
            Assert.AreEqual(new Vector3Int(1, 0, 0), actor.GridPosition);
            Assert.IsNull(GridManager.Instance.GetActorAt(new Vector3Int(0, 0, 0)));
            Assert.AreEqual(actor, GridManager.Instance.GetActorAt(new Vector3Int(1, 0, 0)));
        }

        [Test]
        public void ApplyPositionChange_SameCell_IsNoOp()
        {
            MapManager map = CreateMapWithFloorRadius(2);
            TestActor actor = CreateActor(map, new Vector3Int(0, 0, 0));
            RegisterActor(actor, new Vector3Int(0, 0, 0));

            actor.ApplyPositionChange(new Vector3Int(0, 0, 0));
            Assert.AreEqual(new Vector3Int(0, 0, 0), actor.GridPosition);
            Assert.AreEqual(actor, GridManager.Instance.GetActorAt(new Vector3Int(0, 0, 0)));
        }

        [Test]
        public void ApplyPositionChange_WhenNewTileOccupiedByOther_RevertsAndKeepsOldPosition()
        {
            MapManager map = CreateMapWithFloorRadius(3);
            TestActor mover = CreateActor(map, new Vector3Int(0, 0, 0));
            TestActor blocker = CreateActor(map, new Vector3Int(1, 0, 0));
            RegisterActor(mover, new Vector3Int(0, 0, 0));
            RegisterActor(blocker, new Vector3Int(1, 0, 0));

            // GridMover uses TryMoveRegistration: blocked destination returns false without RegisterActor,
            // so there is no [GRID-CONFLICT] log — only the revert warning from ApplyPositionChange.
            LogAssert.Expect(LogType.Warning, new Regex(@"\[MOVE-ABORTED\].*could not claim \(1, 0, 0\)"));

            mover.ApplyPositionChange(new Vector3Int(1, 0, 0));

            Assert.AreEqual(new Vector3Int(0, 0, 0), mover.GridPosition);
            Assert.AreEqual(blocker, GridManager.Instance.GetActorAt(new Vector3Int(1, 0, 0)));
            Assert.AreEqual(mover, GridManager.Instance.GetActorAt(new Vector3Int(0, 0, 0)));
        }

        [Test]
        public void TryMove_TaggedPlayer_WhenNotPlayerTurn_ReturnsFalse()
        {
            MapManager map = CreateMapWithFloorRadius(3);
            CreateTurnManager(GameState.ENEMY_TURN);
            TestActor actor = CreateActor(map, new Vector3Int(0, 0, 0));
            actor.gameObject.tag = "Player";
            RegisterActor(actor, new Vector3Int(0, 0, 0));

            Assert.IsFalse(actor.TryMove(Vector3Int.right));
            Assert.AreEqual(new Vector3Int(0, 0, 0), actor.GridPosition);
        }

        [Test]
        public void TryMove_TaggedPlayer_OnPlayerTurn_WalksWhenClear()
        {
            MapManager map = CreateMapWithFloorRadius(3);
            CreateTurnManager(GameState.PLAYER_TURN);
            TestActor actor = CreateActor(map, new Vector3Int(0, 0, 0));
            actor.gameObject.tag = "Player";
            RegisterActor(actor, new Vector3Int(0, 0, 0));

            Assert.IsTrue(actor.TryMove(Vector3Int.right));
            Assert.AreEqual(new Vector3Int(1, 0, 0), actor.GridPosition);
        }

        [Test]
        public void TryMove_IntoNonPartyActor_ReturnsTrue_WithoutMovingMover()
        {
            MapManager map = CreateMapWithFloorRadius(3);
            CreatePartyManagerEmpty();
            TestActor mover = CreateActor(map, new Vector3Int(0, 0, 0));
            TestActor other = CreateActor(map, new Vector3Int(1, 0, 0));
            RegisterActor(mover, new Vector3Int(0, 0, 0));
            RegisterActor(other, new Vector3Int(1, 0, 0));

            Assert.IsTrue(mover.TryMove(Vector3Int.right));
            Assert.AreEqual(new Vector3Int(0, 0, 0), mover.GridPosition);
            Assert.AreEqual(other, GridManager.Instance.GetActorAt(new Vector3Int(1, 0, 0)));
        }

        [Test]
        public void TryMove_IntoPartyMember_ReturnsFalse_CurrentImplementation()
        {
            MapManager map = CreateMapWithFloorRadius(3);
            PartyManager party = CreatePartyManagerEmpty();
            TestActor mover = CreateActor(map, new Vector3Int(0, 0, 0));
            TestActor ally = CreateActor(map, new Vector3Int(1, 0, 0));
            party.partyMembers = new List<BaseActor> { ally };
            RegisterActor(mover, new Vector3Int(0, 0, 0));
            RegisterActor(ally, new Vector3Int(1, 0, 0));

            Assert.IsFalse(mover.TryMove(Vector3Int.right));
            Assert.AreEqual(new Vector3Int(0, 0, 0), mover.GridPosition);
        }

        [Test]
        public void GetSmartStepTowards_PrefersAxisWithLargerDelta()
        {
            MapManager map = CreateMapWithFloorRadius(5);
            TestActor actor = CreateActor(map, new Vector3Int(0, 0, 0));
            actor.SetGridPosition(new Vector3Int(0, 0, 0));

            Vector3Int stepCell = actor.GetSmartStepTowards(new Vector3Int(4, 1, 0));
            Assert.AreEqual(new Vector3Int(1, 0, 0), stepCell);
        }

        [Test]
        public void TakeDamage_ReducesCurrentHp()
        {
            TestActor actor = CreateActorOnWalkableGrid(Vector3Int.zero);
            actor.stats.currentHP = 50;
            GameObject source = new GameObject("DamageSource");
            _createdObjects.Add(source);

            actor.TakeDamage(12, source);

            // Default stats: blunt damage reduced by ArmorClass / 5 (see CharacterStats.ArmorClass).
            Assert.AreEqual(40, actor.stats.currentHP);
        }

        [Test]
        public void TakeDamage_WhenHpDropsToZeroOrBelow_CallsDie()
        {
            TestActor actor = CreateActorOnWalkableGrid(Vector3Int.zero);
            actor.stats.currentHP = 3;
            GameObject source = new GameObject("DamageSource2");
            _createdObjects.Add(source);

            actor.TakeDamage(10, source);

            Assert.LessOrEqual(actor.stats.currentHP, 0);
            Assert.AreEqual(1, actor.DieInvocationCount);
        }

        private TestActor CreateActorOnWalkableGrid(Vector3Int gridPos)
        {
            MapManager map = CreateMapWithFloorRadius(6);
            TestActor actor = CreateActor(map, gridPos);
            RegisterActor(actor, gridPos);
            return actor;
        }

        private static void RegisterActor(TestActor actor, Vector3Int gridPos)
        {
            actor.SetGridPosition(gridPos);
            actor.transform.position = new Vector3(gridPos.x, gridPos.y, 0f);
            BindGridMoverSelf(actor);
            // Clear any stale cell entry so RegisterActor and GridMover.self agree on the same IBattleTarget reference.
            GridManager.Instance.UnregisterActor(gridPos);
            IBattleTarget battleSelf = actor.GetComponent<IBattleTarget>();
            Assert.IsNotNull(battleSelf, "Test actor must implement IBattleTarget for grid registration.");
            GridManager.Instance.RegisterActor(gridPos, battleSelf);
        }

        private TestActor CreateActor(MapManager map, Vector3Int gridPos)
        {
            GameObject go = new GameObject($"BaseActorTest_{gridPos.x}_{gridPos.y}");
            _createdObjects.Add(go);
            go.AddComponent<TestQuietEssenceSlotManager>();
            TestActor actor = go.AddComponent<TestActor>();
            InjectMapManager(actor, map);
            actor.SetGridPosition(gridPos);
            actor.transform.position = new Vector3(gridPos.x, gridPos.y, 0f);
            // GridMover.Awake can run before IBattleTarget is resolvable on the same GO; ensure self is bound
            // so RegisterActor always receives a non-null IBattleTarget (avoids NRE in conflict logging).
            BindGridMoverSelf(actor);
            return actor;
        }

        private static void BindGridMoverSelf(BaseActor actor)
        {
            GridMover mover = actor.GetComponent<GridMover>();
            if (mover == null) return;
            FieldInfo selfField = typeof(GridMover).GetField("self", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(selfField, "GridMover should expose a private 'self' IBattleTarget field for test binding.");
            IBattleTarget battleSelf = actor.GetComponent<IBattleTarget>();
            Assert.IsNotNull(battleSelf, "Actor must expose IBattleTarget for GridMover.ApplyPositionChange unregister checks.");
            selfField.SetValue(mover, battleSelf);
        }

        private MapManager CreateMapWithFloorRadius(int radius)
        {
            GameObject mapRoot = new GameObject("Map_BaseActorTest");
            _createdObjects.Add(mapRoot);
            MapManager mapManager = mapRoot.AddComponent<MapManager>();

            GameObject gridRoot = new GameObject("Grid_BaseActorTest");
            _createdObjects.Add(gridRoot);
            gridRoot.AddComponent<Grid>();

            GameObject floorObject = new GameObject("Floor_BaseActorTest");
            _createdObjects.Add(floorObject);
            floorObject.transform.SetParent(gridRoot.transform);
            Tilemap floorMap = floorObject.AddComponent<Tilemap>();
            floorObject.AddComponent<TilemapRenderer>();

            GameObject wallObject = new GameObject("Wall_BaseActorTest");
            _createdObjects.Add(wallObject);
            wallObject.transform.SetParent(gridRoot.transform);
            Tilemap wallMap = wallObject.AddComponent<Tilemap>();
            wallObject.AddComponent<TilemapRenderer>();

            Tile tile = ScriptableObject.CreateInstance<Tile>();
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                    floorMap.SetTile(new Vector3Int(x, y, 0), tile);
            }

            SetPrivateField(mapManager, "floorMap", floorMap);
            SetPrivateField(mapManager, "wallMap", wallMap);

            GameObject gridManagerObject = new GameObject("GridManager_BaseActorTest");
            _createdObjects.Add(gridManagerObject);
            gridManagerObject.AddComponent<GridManager>();

            return mapManager;
        }

        private void CreateTurnManager(GameState state)
        {
            GameObject go = new GameObject("TurnManager_BaseActorTest");
            _createdObjects.Add(go);
            TurnManager tm = go.AddComponent<TurnManager>();
            tm.currentState = state;
        }

        private PartyManager CreatePartyManagerEmpty()
        {
            GameObject go = new GameObject("PartyManager_BaseActorTest");
            _createdObjects.Add(go);
            PartyManager pm = go.AddComponent<PartyManager>();
            pm.partyMembers = new List<BaseActor>();
            return pm;
        }

        private static void InjectMapManager(BaseActor actor, MapManager map)
        {
            FieldInfo field = typeof(BaseActor).GetField("mapManager", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field);
            field.SetValue(actor, map);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            field.SetValue(target, value);
        }

        private sealed class TestActor : BaseActor
        {
            public int DieInvocationCount { get; private set; }

            protected override void Die() => DieInvocationCount++;
        }
    }
}
