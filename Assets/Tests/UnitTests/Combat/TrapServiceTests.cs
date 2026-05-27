using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Actors.Components;
using JRogue.Input;
using JRogue.Manager.Grid;
using JRogue.Manager.Map;
using JRogue.Manager.Party;
using JRogue.Manager.Turn;
using JRogue.Service.Formation;
using JRogue.Stats;
using JRogue.Tests.UnitTests.Input;
using JRogue.Traps;
using JRogue.UI.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.Tests.UnitTests.Combat
{
    [TestFixture]
    public sealed class TrapServiceTests
    {
        readonly List<GameObject> _created = new List<GameObject>();
        readonly List<Object> _assets = new List<Object>();

        [SetUp]
        public void SetUp() => InputTestSceneBuilder.ResetSingletonManagersForTests();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _created)
            {
                if (go != null)
                    Object.DestroyImmediate(go);
            }

            _created.Clear();

            foreach (Object asset in _assets)
            {
                if (asset != null)
                    Object.DestroyImmediate(asset);
            }

            _assets.Clear();

            if (TrapConfirmDialogUI.EnsureInstance() != null)
                Object.DestroyImmediate(TrapConfirmDialogUI.EnsureInstance().gameObject);

            ClearTrapServiceInstance();
            InputTestSceneBuilder.ResetSingletonManagersForTests();
        }

        [Test]
        public void InvisibleFloorTrap_TriggersWithoutConfirm()
        {
            TrapService service = CreateTrapService();
            TrapDefinition spike = CreateSpike(invisible: true);
            Vector3Int trapCell = new Vector3Int(1, 0, 0);
            service.Register(trapCell, spike);

            BaseActor actor = CreateActor(Vector3Int.zero, perception: 5);
            Assert.IsTrue(actor.TryMove(Vector3Int.right));
            Assert.AreEqual(trapCell, actor.GridPosition);
            Assert.IsTrue(service.TryGetFloorTrap(trapCell, out TrapInstance instance));
            Assert.IsTrue(instance.HasTriggered);
        }

        [Test]
        public void VisibleFloorTrap_MoveGateInterceptsBeforeMove()
        {
            TrapService service = CreateTrapService();
            Vector3Int trapCell = new Vector3Int(1, 0, 0);
            service.Register(trapCell, CreateSpike(invisible: false));

            BaseActor actor = CreateActor(Vector3Int.zero, perception: 5);
            bool moved = false;
            Assert.IsTrue(TrapMoveGate.TryInterceptMove(
                actor,
                trapCell,
                isEnemyBump: false,
                () => moved = true));
            Assert.IsFalse(moved);
            Assert.AreEqual(Vector3Int.zero, actor.GridPosition);
            Assert.IsTrue(service.RequiresEnterConfirm(trapCell));
        }

        [Test]
        public void BearTrap_DamagesOnceThenSilent()
        {
            TrapService service = CreateTrapService();
            Vector3Int trapCell = new Vector3Int(0, 0, 0);
            service.Register(trapCell, CreateBear());

            BaseActor actor = CreateActor(trapCell, perception: 5);
            HealthComponent health = actor.GetComponent<HealthComponent>();
            int startHp = actor.stats.currentHP;

            service.TryTriggerFloorTrap(actor, trapCell);
            Assert.Less(actor.stats.currentHP, startHp);

            int afterFirst = actor.stats.currentHP;
            service.TryTriggerFloorTrap(actor, trapCell);
            Assert.AreEqual(afterFirst, actor.stats.currentHP);
        }

        [Test]
        public void DartTrap_FiresFromAdjacentFloor_ThreeTimes()
        {
            InputTestSceneBuilder.SetupMapAndManagers(_created);
            MapManager map = MapManager.Instance;
            Tilemap wallMap = map.WallMap;
            Tilemap floorMap = map.FloorMap;

            Vector3Int wallHost = new Vector3Int(2, 0, 0);
            Vector3Int triggerCell = new Vector3Int(1, 0, 0);
            wallMap.SetTile(wallHost, ScriptableObject.CreateInstance<Tile>());
            floorMap.SetTile(triggerCell, ScriptableObject.CreateInstance<Tile>());

            TrapService service = CreateTrapService();
            service.Register(wallHost, CreateDart());

            BaseActor actor = CreateActor(triggerCell, perception: 5);
            int startHp = actor.stats.currentHP;

            for (int i = 0; i < 3; i++)
            {
                int before = actor.stats.currentHP;
                service.TryTriggerWallTraps(actor, triggerCell);
                Assert.Less(actor.stats.currentHP, before);
            }

            int afterThree = actor.stats.currentHP;
            service.TryTriggerWallTraps(actor, triggerCell);
            Assert.AreEqual(afterThree, actor.stats.currentHP);
        }

        [Test]
        public void HighPerception_RevealsInvisibleTrapBeforeStep()
        {
            TrapService service = CreateTrapService();
            Vector3Int trapCell = new Vector3Int(5, 0, 0);
            service.Register(trapCell, CreateSpike(invisible: true));

            CreateActor(new Vector3Int(0, 0, 0), perception: 20);
            service.EvaluateDetection();

            Assert.IsTrue(service.TryGetFloorTrap(trapCell, out TrapInstance instance));
            Assert.IsTrue(instance.IsDetected);
            Assert.IsTrue(service.IsVisibleFloorTrapAt(trapCell));
        }

        [Test]
        public void FormationFollower_AvoidsVisibleFloorTrap()
        {
            InputTestSceneBuilder.SetupMapAndManagers(_created);
            TrapService service = CreateTrapService();
            Vector3Int trapCell = new Vector3Int(2, 0, 0);
            service.Register(trapCell, CreateSpike(invisible: false));

            MapManager map = MapManager.Instance;
            GridManager grid = GridManager.Instance;
            BaseActor follower = CreateActor(Vector3Int.zero, perception: 5);

            Assert.IsFalse(FormationRushService.IsValidMove(
                map,
                grid,
                trapCell,
                new Dictionary<BaseActor, Vector3Int>(),
                allowAllies: false,
                follower: follower));
        }

        [Test]
        public void ExhaustedDartTrigger_NoConfirm_AndFollowerMayEnter()
        {
            InputTestSceneBuilder.SetupMapAndManagers(_created);
            MapManager map = MapManager.Instance;
            Tilemap wallMap = map.WallMap;
            Tilemap floorMap = map.FloorMap;

            Vector3Int wallHost = new Vector3Int(2, 0, 0);
            Vector3Int triggerCell = new Vector3Int(1, 0, 0);
            wallMap.SetTile(wallHost, ScriptableObject.CreateInstance<Tile>());
            floorMap.SetTile(triggerCell, ScriptableObject.CreateInstance<Tile>());

            TrapService service = CreateTrapService();
            service.Register(wallHost, CreateDart());
            Assert.IsTrue(service.TryGetWallTrap(wallHost, out TrapInstance dart));

            BaseActor actor = CreateActor(triggerCell, perception: 5);
            for (int i = 0; i < 3; i++)
                service.TryTriggerWallTraps(actor, triggerCell);

            Assert.IsFalse(dart.CanFire());
            Assert.IsTrue(service.IsVisibleWallTrapTriggerAt(triggerCell));
            Assert.IsFalse(service.IsPathingAvoidCell(triggerCell));
            Assert.IsFalse(service.RequiresEnterConfirm(triggerCell));

            bool moved = false;
            Assert.IsFalse(TrapMoveGate.TryInterceptMove(
                actor,
                triggerCell,
                isEnemyBump: false,
                () => moved = true));

            GridManager grid = GridManager.Instance;
            BaseActor follower = CreateActor(Vector3Int.zero, perception: 5);
            Assert.IsTrue(FormationRushService.IsValidMove(
                map,
                grid,
                triggerCell,
                new Dictionary<BaseActor, Vector3Int>(),
                allowAllies: false,
                follower: follower));
        }

        [Test]
        public void ExhaustedBearTrap_NoConfirm_AndFollowerMayEnter()
        {
            InputTestSceneBuilder.SetupMapAndManagers(_created);
            TrapService service = CreateTrapService();
            Vector3Int trapCell = new Vector3Int(2, 0, 0);
            service.Register(trapCell, CreateBear());
            Assert.IsTrue(service.TryGetFloorTrap(trapCell, out TrapInstance bear));

            BaseActor actor = CreateActor(trapCell, perception: 5);
            service.TryTriggerFloorTrap(actor, trapCell);

            Assert.IsFalse(bear.CanFire());
            Assert.IsTrue(service.IsVisibleFloorTrapAt(trapCell));
            Assert.IsFalse(service.IsPathingAvoidCell(trapCell));
            Assert.IsFalse(service.RequiresEnterConfirm(trapCell));

            MapManager map = MapManager.Instance;
            GridManager grid = GridManager.Instance;
            BaseActor follower = CreateActor(Vector3Int.zero, perception: 5);
            Assert.IsTrue(FormationRushService.IsValidMove(
                map,
                grid,
                trapCell,
                new Dictionary<BaseActor, Vector3Int>(),
                allowAllies: false,
                follower: follower));
        }

        [Test]
        public void FormationFollower_AvoidsVisibleWallDartTrigger()
        {
            InputTestSceneBuilder.SetupMapAndManagers(_created);
            MapManager map = MapManager.Instance;
            Tilemap wallMap = map.WallMap;
            Tilemap floorMap = map.FloorMap;

            Vector3Int wallHost = new Vector3Int(2, 0, 0);
            Vector3Int triggerCell = new Vector3Int(1, 0, 0);
            wallMap.SetTile(wallHost, ScriptableObject.CreateInstance<Tile>());
            floorMap.SetTile(triggerCell, ScriptableObject.CreateInstance<Tile>());

            TrapService service = CreateTrapService();
            service.Register(wallHost, CreateDart());

            GridManager grid = GridManager.Instance;
            BaseActor follower = CreateActor(Vector3Int.zero, perception: 5);

            Assert.IsTrue(service.IsVisibleWallTrapTriggerAt(triggerCell));
            Assert.IsFalse(FormationRushService.IsValidMove(
                map,
                grid,
                triggerCell,
                new Dictionary<BaseActor, Vector3Int>(),
                allowAllies: false,
                follower: follower));
        }

        [Test]
        public void VisibleWallDartTrigger_MoveGateInterceptsBeforeMove()
        {
            InputTestSceneBuilder.SetupMapAndManagers(_created);
            MapManager map = MapManager.Instance;
            Tilemap wallMap = map.WallMap;
            Tilemap floorMap = map.FloorMap;

            Vector3Int wallHost = new Vector3Int(2, 0, 0);
            Vector3Int triggerCell = new Vector3Int(1, 0, 0);
            wallMap.SetTile(wallHost, ScriptableObject.CreateInstance<Tile>());
            floorMap.SetTile(triggerCell, ScriptableObject.CreateInstance<Tile>());

            TrapService service = CreateTrapService();
            service.Register(wallHost, CreateDart());

            BaseActor actor = CreateActor(Vector3Int.zero, perception: 5);
            bool moved = false;
            Assert.IsTrue(TrapMoveGate.TryInterceptMove(
                actor,
                triggerCell,
                isEnemyBump: false,
                () => moved = true));
            Assert.IsFalse(moved);
            Assert.AreEqual(Vector3Int.zero, actor.GridPosition);
            Assert.IsTrue(service.RequiresEnterConfirm(triggerCell));
        }

        [Test]
        public void VisibleWallDartTrigger_FormationLeader_ShowsConfirmThenMovesOnYes()
        {
            InputTestSceneBuilder.SetupMapAndManagers(_created);
            MapManager map = MapManager.Instance;
            Tilemap wallMap = map.WallMap;
            Tilemap floorMap = map.FloorMap;

            Vector3Int wallHost = new Vector3Int(0, 0, 0);
            Vector3Int triggerCell = new Vector3Int(1, 0, 0);
            wallMap.SetTile(wallHost, ScriptableObject.CreateInstance<Tile>());
            floorMap.SetTile(triggerCell, ScriptableObject.CreateInstance<Tile>());

            TrapService service = CreateTrapService();
            service.Register(wallHost, CreateDart());

            PartyManager party = InputTestSceneBuilder.CreatePartyWithTestActors(1, _created);
            InputTestSceneBuilder.SetPrivateField(party, "isFormationActive", true);
            BaseActor leader = party.partyMembers[0];
            leader.stats.currentHP = leader.stats.MaxHP;
            InputTestSceneBuilder.RegisterCurrentPartyOnGrid(party.partyMembers);

            var processor = new PlayerCommandProcessor();
            TurnManager.Instance.currentState = GameState.PLAYER_TURN;
            Vector3Int start = leader.GridPosition;

            Assert.IsTrue(processor.TryApply(PlayerCommand.MoveGrid(Vector3Int.right)));
            Assert.AreEqual(start, leader.GridPosition);
            Assert.IsTrue(TrapConfirmDialogUI.BlocksGameplay);

            CommitTrapYes();
            Assert.AreEqual(triggerCell, leader.GridPosition);
            Assert.Less(leader.stats.currentHP, leader.stats.MaxHP);
        }

        [Test]
        public void FormationLeader_CanPlanMoveOntoVisibleWallDartTrigger()
        {
            InputTestSceneBuilder.SetupMapAndManagers(_created);
            MapManager map = MapManager.Instance;
            Tilemap wallMap = map.WallMap;
            Tilemap floorMap = map.FloorMap;

            Vector3Int wallHost = new Vector3Int(2, 0, 0);
            Vector3Int triggerCell = new Vector3Int(1, 0, 0);
            wallMap.SetTile(wallHost, ScriptableObject.CreateInstance<Tile>());
            floorMap.SetTile(triggerCell, ScriptableObject.CreateInstance<Tile>());

            TrapService service = CreateTrapService();
            service.Register(wallHost, CreateDart());

            GridManager grid = GridManager.Instance;
            BaseActor leader = CreateActor(Vector3Int.zero, perception: 5);

            Assert.IsTrue(FormationRushService.IsValidMove(
                map,
                grid,
                triggerCell,
                new Dictionary<BaseActor, Vector3Int>(),
                allowAllies: true,
                follower: leader));
        }

        [Test]
        public void VisibleFloorTrap_FormationLeader_ShowsConfirmThenMovesOnYes()
        {
            InputTestSceneBuilder.SetupMapAndManagers(_created);
            TrapService service = CreateTrapService();
            PartyManager party = InputTestSceneBuilder.CreatePartyWithTestActors(1, _created);
            InputTestSceneBuilder.SetPrivateField(party, "isFormationActive", true);
            BaseActor leader = party.partyMembers[0];
            leader.stats.currentHP = leader.stats.MaxHP;
            InputTestSceneBuilder.RegisterCurrentPartyOnGrid(party.partyMembers);

            Vector3Int trapCell = leader.GridPosition + Vector3Int.right;
            service.Register(trapCell, CreateSpike(invisible: false));

            var processor = new PlayerCommandProcessor();
            TurnManager.Instance.currentState = GameState.PLAYER_TURN;
            Vector3Int start = leader.GridPosition;

            Assert.IsTrue(processor.TryApply(PlayerCommand.MoveGrid(Vector3Int.right)));
            Assert.AreEqual(start, leader.GridPosition);
            Assert.IsTrue(TrapConfirmDialogUI.BlocksGameplay);

            CommitTrapYes();
            Assert.AreEqual(trapCell, leader.GridPosition);
            Assert.IsTrue(service.TryGetFloorTrap(trapCell, out TrapInstance instance));
            Assert.IsTrue(instance.HasTriggered);
        }

        [Test]
        public void VisibleFloorTrap_PlayerCommand_ConfirmCancel_DoesNotMove()
        {
            InputTestSceneBuilder.SetupMapAndManagers(_created);
            TrapService service = CreateTrapService();
            PartyManager party = InputTestSceneBuilder.CreatePartyWithTestActors(1, _created);
            BaseActor leader = party.partyMembers[0];
            leader.stats.currentHP = leader.stats.MaxHP;
            InputTestSceneBuilder.RegisterCurrentPartyOnGrid(party.partyMembers);

            Vector3Int trapCell = leader.GridPosition + Vector3Int.right;
            service.Register(trapCell, CreateSpike(invisible: false));

            var processor = new PlayerCommandProcessor();
            TurnManager.Instance.currentState = GameState.PLAYER_TURN;
            Vector3Int start = leader.GridPosition;

            Assert.IsTrue(processor.TryApply(PlayerCommand.MoveGrid(Vector3Int.right)));
            Assert.AreEqual(start, leader.GridPosition);
            Assert.IsTrue(TrapConfirmDialogUI.BlocksGameplay);
        }

        TrapService CreateTrapService()
        {
            InputTestSceneBuilder.SetupMapAndManagers(_created);
            var go = new GameObject("TrapService_Test");
            _created.Add(go);
            var overlayGo = new GameObject("TrapOverlay_Test");
            _created.Add(overlayGo);
            Tilemap overlay = overlayGo.AddComponent<Tilemap>();
            TrapService service = go.AddComponent<TrapService>();
            service.SetOverlayMap(overlay);
            return service;
        }

        TrapDefinition CreateSpike(bool invisible)
        {
            var def = ScriptableObject.CreateInstance<TrapDefinition>();
            _assets.Add(def);
            def.trapId = TrapId.Spike;
            def.displayName = "Spike Trap";
            def.placement = TrapPlacement.Floor;
            def.initialVisibility = invisible ? TrapVisibility.Invisible : TrapVisibility.Visible;
            def.triggerLimit = TrapTriggerLimit.Infinite;
            def.piercingDamage = 8;
            def.detectionThreshold = 12;
            return def;
        }

        TrapDefinition CreateBear()
        {
            var def = ScriptableObject.CreateInstance<TrapDefinition>();
            _assets.Add(def);
            def.trapId = TrapId.Bear;
            def.displayName = "Bear Trap";
            def.placement = TrapPlacement.Floor;
            def.initialVisibility = TrapVisibility.Visible;
            def.triggerLimit = TrapTriggerLimit.Once;
            def.piercingDamage = 15;
            return def;
        }

        TrapDefinition CreateDart()
        {
            var def = ScriptableObject.CreateInstance<TrapDefinition>();
            _assets.Add(def);
            def.trapId = TrapId.Dart;
            def.displayName = "Dart Trap";
            def.placement = TrapPlacement.Wall;
            def.initialVisibility = TrapVisibility.Visible;
            def.triggerLimit = TrapTriggerLimit.Finite;
            def.finiteCharges = 3;
            def.triggerRange = 1;
            def.piercingDamage = 10;
            return def;
        }

        BaseActor CreateActor(Vector3Int gridPos, int perception)
        {
            GameObject go = new GameObject("Actor");
            _created.Add(go);
            var stats = go.AddComponent<CharacterStats>();
            stats.Skills[SkillType.Perception] = new Stat(perception);
            stats.currentHP = stats.MaxHP;
            go.AddComponent<HealthComponent>();
            var mover = go.AddComponent<GridMover>();
            var actor = go.AddComponent<BaseActor>();
            actor.stats = stats;
            GridManager.Instance.RegisterActor(gridPos, actor);
            mover.ApplyPositionChange(gridPos);
            return actor;
        }

        static void ClearTrapServiceInstance()
        {
            TrapService existing = Object.FindAnyObjectByType<TrapService>();
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);
        }

        static void CommitTrapYes()
        {
            var commit = typeof(TrapConfirmDialogUI).GetMethod(
                "CommitYes",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            commit.Invoke(TrapConfirmDialogUI.EnsureInstance(), null);
        }
    }
}
