using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using JRogue.Actors;
using JRogue.Manager.Grid;
using JRogue.Manager.Map;
using JRogue.Manager.Party;
using JRogue.Manager.Turn;
using JRogue.Tests.UnitTests.MockMonoBehavior;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

namespace JRogue.Tests.UnitTests.Input
{
    [TestFixture]
    public class InputHandlerTest
    {
        private readonly List<GameObject> _createdObjects = new List<GameObject>();
        private MethodInfo _processFollowerRushMethod;

        [SetUp]
        public void SetUp()
        {
            _processFollowerRushMethod = typeof(JRogue.Input.InputHandler)
                .GetMethod("ProcessFollowerRush", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(_processFollowerRushMethod, "Expected private method ProcessFollowerRush to exist.");
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject createdObject in _createdObjects)
            {
                if (createdObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(createdObject);
                }
            }

            _createdObjects.Clear();
            PartyManager.Instance = null;
            TurnManager.Instance = null;
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        public void ProcessFollowerRush_PartySizesOneToSix_FollowersMoveToHistorySlots(int partySize)
        {
            TestFixtureContext context = CreateFixture(partySize);
            BaseActor leader = context.PartyManager.partyMembers[0];

            leader.SetGridPosition(new Vector3Int(0, 0, 0));
            context.PartyManager.positionHistory = BuildTrailHistory(context.PartyManager.partyMembers);
            RegisterCurrentPartyOnGrid(context.PartyManager.partyMembers);

            InvokeProcessFollowerRush(context.InputHandler);

            Assert.AreEqual(new Vector3Int(0, 0, 0), leader.GridPosition);
            for (int i = 1; i < partySize; i++)
            {
                Assert.AreEqual(context.PartyManager.positionHistory[i], context.PartyManager.partyMembers[i].GridPosition);
            }

            AssertAllPositionsUnique(context.PartyManager.partyMembers.Select(m => m.GridPosition).ToList());
            AssertRushLeavesFreshPlayerTurn(context.PartyManager.partyMembers, context.TurnManager);
        }

        [Test]
        public void ProcessFollowerRush_HistoryTooShort_MissingSlotsStayInPlace()
        {
            TestFixtureContext context = CreateFixture(4);
            List<BaseActor> members = context.PartyManager.partyMembers;

            members[0].SetGridPosition(new Vector3Int(0, 0, 0));
            members[1].SetGridPosition(new Vector3Int(0, -1, 0));
            members[2].SetGridPosition(new Vector3Int(0, -2, 0));
            members[3].SetGridPosition(new Vector3Int(0, -3, 0));

            // Two history entries for four members: indices 2+ fall back to each follower's current cell.
            // Slot 1 must not duplicate the leader's tile — occupancy rules prevent stacking on (0,0,0).
            context.PartyManager.positionHistory = new List<Vector3Int>
            {
                new Vector3Int(0, 0, 0),
                new Vector3Int(0, -1, 0)
            };
            RegisterCurrentPartyOnGrid(members);

            InvokeProcessFollowerRush(context.InputHandler);

            Assert.AreEqual(new Vector3Int(0, 0, 0), members[0].GridPosition);
            Assert.AreEqual(new Vector3Int(0, -1, 0), members[1].GridPosition);
            Assert.AreEqual(new Vector3Int(0, -2, 0), members[2].GridPosition);
            Assert.AreEqual(new Vector3Int(0, -3, 0), members[3].GridPosition);
            AssertRushLeavesFreshPlayerTurn(members, context.TurnManager);
        }

        [Test]
        public void ProcessFollowerRush_TargetFarAway_FollowerMovesMaxTwoTilesTowardTarget()
        {
            TestFixtureContext context = CreateFixture(2);
            BaseActor leader = context.PartyManager.partyMembers[0];
            BaseActor follower = context.PartyManager.partyMembers[1];

            leader.SetGridPosition(new Vector3Int(0, 0, 0));
            follower.SetGridPosition(new Vector3Int(0, -5, 0));
            context.PartyManager.positionHistory = new List<Vector3Int>
            {
                new Vector3Int(0, 0, 0),
                new Vector3Int(0, 0, 0)
            };
            RegisterCurrentPartyOnGrid(context.PartyManager.partyMembers);

            InvokeProcessFollowerRush(context.InputHandler);

            Assert.AreEqual(new Vector3Int(0, -3, 0), follower.GridPosition);
            AssertRushLeavesFreshPlayerTurn(context.PartyManager.partyMembers, context.TurnManager);
        }

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        public void FormationSequence_OneLeaderStep_HistoryShiftsAndFollowersRush(int partySize)
        {
            TestFixtureContext context = CreateFixture(partySize);
            List<BaseActor> members = context.PartyManager.partyMembers;
            BaseActor leader = members[0];

            RegisterCurrentPartyOnGrid(members);
            context.PartyManager.SnapHistoryToCurrentPositions();

            Vector3Int originalLeaderPos = leader.GridPosition;
            Vector3Int moveDirection = Vector3Int.right;

            Assert.IsTrue(leader.TryMove(moveDirection), "Leader should be able to move one tile on walkable floor.");
            Assert.AreEqual(originalLeaderPos + moveDirection, leader.GridPosition);

            context.PartyManager.RecordNewLeaderPosition(leader.GridPosition);
            InvokeProcessFollowerRush(context.InputHandler);

            Assert.AreEqual(leader.GridPosition, context.PartyManager.positionHistory[0]);
            Assert.AreEqual(originalLeaderPos, context.PartyManager.positionHistory[1]);
            for (int i = 1; i < members.Count; i++)
            {
                Assert.AreEqual(context.PartyManager.positionHistory[i], members[i].GridPosition);
            }

            AssertAllPositionsUnique(members.Select(m => m.GridPosition).ToList());
            AssertRushLeavesFreshPlayerTurn(members, context.TurnManager);
        }

        [Test]
        public void FormationSequence_LeaderStationary_HistoryUnchangedFollowersStillRush()
        {
            TestFixtureContext context = CreateFixture(3);
            List<BaseActor> members = context.PartyManager.partyMembers;

            members[0].SetGridPosition(new Vector3Int(0, 0, 0));
            members[1].SetGridPosition(new Vector3Int(0, -2, 0));
            members[2].SetGridPosition(new Vector3Int(0, -4, 0));
            RegisterCurrentPartyOnGrid(members);

            context.PartyManager.positionHistory = new List<Vector3Int>
            {
                new Vector3Int(0, 0, 0),
                new Vector3Int(0, -1, 0),
                new Vector3Int(0, -2, 0)
            };
            List<Vector3Int> historyBefore = new List<Vector3Int>(context.PartyManager.positionHistory);

            // Mimics OnMove branch where the leader attacked/bumped and position did not change.
            UnityEngine.TestTools.LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex(@"\[TurnManager\] InputPartyActor_1 has acted\..*"));
            UnityEngine.TestTools.LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex(@"\[TurnManager\] InputPartyActor_2 has acted\..*"));
            UnityEngine.TestTools.LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex(@"\[TurnManager\] InputPartyActor_0 has acted\..*"));
            context.PartyManager.RecordNewLeaderPosition(members[0].GridPosition);
            InvokeProcessFollowerRush(context.InputHandler);

            CollectionAssert.AreEqual(historyBefore, context.PartyManager.positionHistory);
            Assert.AreEqual(new Vector3Int(0, -1, 0), members[1].GridPosition);
            Assert.AreEqual(new Vector3Int(0, -2, 0), members[2].GridPosition);
        }

        /// <summary>
        /// Mirrors <see cref="JRogue.Input.InputHandler"/> rush planning while the spatial hash only holds the leader
        /// (followers unregistered until land). Validates scenarios with stale breadcrumbs and partial convergence across turns.
        /// </summary>
        [Test]
        public void FormationSequence_StaleHistoryRecordLogsSanity_PartialRush_MinimalSmoke()
        {
            TestFixtureContext context = CreateFixture(3);
            List<BaseActor> members = context.PartyManager.partyMembers;
            PartyManager partyManager = context.PartyManager;

            members[0].SetGridPosition(new Vector3Int(-5, 0, 0));
            members[1].SetGridPosition(new Vector3Int(1, -1, 0));
            members[2].SetGridPosition(new Vector3Int(1, -2, 0));
            partyManager.positionHistory = new List<Vector3Int>
            {
                new Vector3Int(-4, 0, 0),
                new Vector3Int(0, 0, 0),
                new Vector3Int(1, -1, 0)
            };
            RegisterCurrentPartyOnGrid(members);

            LogAssert.Expect(LogType.Error, new Regex(@"\[SANITY-FAIL\].*"));

            Vector3Int leaderExpected = members[0].GridPosition;
            partyManager.RecordNewLeaderPosition(members[0].GridPosition);
            InvokeProcessFollowerRush(context.InputHandler);

            Assert.AreEqual(3, partyManager.positionHistory.Count);
            Assert.AreEqual(leaderExpected, members[0].GridPosition, "Leader should not be displaced by the rush sweep.");
            AssertAllPositionsUnique(members.Select(m => m.GridPosition).ToList());
            AssertRushLeavesFreshPlayerTurn(members, context.TurnManager);
        }

        [Test]
        public void FormationSequence_WideLagAfterLeaderSeparatedFromColumn_MinimalSmoke()
        {
            TestFixtureContext context = CreateFixture(3);
            List<BaseActor> members = context.PartyManager.partyMembers;
            PartyManager partyManager = context.PartyManager;

            members[0].SetGridPosition(new Vector3Int(-10, 0, 0));
            members[1].SetGridPosition(new Vector3Int(0, -4, 0));
            members[2].SetGridPosition(new Vector3Int(2, -3, 0));
            RegisterCurrentPartyOnGrid(members);
            partyManager.SnapHistoryToCurrentPositions();

            Vector3Int leaderBeforeHist0 = partyManager.positionHistory[0];
            Vector3Int moveAxis = Vector3Int.left;
            Assert.IsTrue(members[0].TryMove(moveAxis), "Leader advance should succeed on blank floor.");

            Vector3Int leaderExpected = members[0].GridPosition;
            Assert.AreEqual(leaderBeforeHist0 + moveAxis, leaderExpected);

            LogAssert.Expect(LogType.Error, new Regex(@"\[SANITY-FAIL\].*"));
            partyManager.RecordNewLeaderPosition(members[0].GridPosition);
            InvokeProcessFollowerRush(context.InputHandler);

            Assert.AreEqual(3, partyManager.positionHistory.Count);
            Assert.AreEqual(leaderExpected, members[0].GridPosition);
            Assert.IsTrue(
                members[1].GridPosition != leaderExpected && members[2].GridPosition != leaderExpected,
                "Followers should not teleport onto the leader tile in one rush.");
            AssertAllPositionsUnique(members.Select(m => m.GridPosition).ToList());
            AssertRushLeavesFreshPlayerTurn(members, context.TurnManager);
        }

        /// <summary>
        /// Duplicates <see cref="JRogue.Input.InputHandler.ProcessFollowerRush"/> follower planning versus leader lock and walkable tiles;
        /// asserts simulated landing equals actual and each follower closes &lt;= 2 tiles (Euclidean) toward its breadcrumb.
        /// </summary>
        [Test]
        public void ProcessFollowerRush_Explicit_VerticalColumn_RemainingTowardHistoricSlotsMatched()
        {
            TestFixtureContext context = CreateFixture(3);
            List<BaseActor> members = context.PartyManager.partyMembers;
            PartyManager partyManager = context.PartyManager;

            members[0].SetGridPosition(new Vector3Int(0, -1, 0));
            members[1].SetGridPosition(new Vector3Int(0, -8, 0));
            members[2].SetGridPosition(new Vector3Int(0, -13, 0));
            RegisterCurrentPartyOnGrid(members);
            partyManager.SnapHistoryToCurrentPositions();

            Assert.IsTrue(members[0].TryMove(Vector3Int.down));
            Vector3Int leaderGrid = members[0].GridPosition;

            var followerStarts = new Dictionary<BaseActor, Vector3Int>();
            for (int i = 1; i < members.Count; i++)
                followerStarts[members[i]] = members[i].GridPosition;

            LogAssert.Expect(LogType.Error, new Regex(@"\[SANITY-FAIL\].*"));
            partyManager.RecordNewLeaderPosition(members[0].GridPosition);
            List<Vector3Int> historyAfterRecord = partyManager.positionHistory;

            Dictionary<BaseActor, Vector3Int> predicted =
                SimulateFollowerRushPlans(members, historyAfterRecord, leaderGrid);

            InvokeProcessFollowerRush(context.InputHandler);

            for (int i = 1; i < members.Count; i++)
            {
                BaseActor follower = members[i];
                Vector3Int historicalTarget =
                    i < historyAfterRecord.Count ? historyAfterRecord[i] : follower.GridPosition;

                Assert.AreEqual(
                    predicted[follower],
                    follower.GridPosition,
                    $"Follower {follower.name} should match deterministic rush planner.");

                float beforeDist = Vector3Int.Distance(followerStarts[follower], historicalTarget);
                float afterDist = Vector3Int.Distance(follower.GridPosition, historicalTarget);
                Assert.LessOrEqual(afterDist, beforeDist);
                Assert.LessOrEqual(beforeDist - afterDist, 2.001f,
                    "Burst toward breadcrumb capped at roughly two Euclidean tiles.");
            }

            AssertAllPositionsUnique(members.Select(m => m.GridPosition).ToList());
            AssertRushLeavesFreshPlayerTurn(members, context.TurnManager);
        }

        [Test]
        public void ProcessFollowerRush_Explicit_WestAlignedRow_ReducesTowardHistoricWithinBurstCap()
        {
            TestFixtureContext context = CreateFixture(3);
            List<BaseActor> members = context.PartyManager.partyMembers;
            PartyManager partyManager = context.PartyManager;

            members[0].SetGridPosition(new Vector3Int(-22, 0, 0));
            members[1].SetGridPosition(new Vector3Int(-40, 0, 0));
            members[2].SetGridPosition(new Vector3Int(-55, 0, 0));
            RegisterCurrentPartyOnGrid(members);
            partyManager.SnapHistoryToCurrentPositions();

            Assert.IsTrue(members[0].TryMove(Vector3Int.right));
            Vector3Int leaderGrid = members[0].GridPosition;

            var followerStarts = new Dictionary<BaseActor, Vector3Int>();
            for (int i = 1; i < members.Count; i++)
                followerStarts[members[i]] = members[i].GridPosition;

            LogAssert.Expect(LogType.Error, new Regex(@"\[SANITY-FAIL\].*"));
            partyManager.RecordNewLeaderPosition(members[0].GridPosition);
            List<Vector3Int> historyAfterRecord = partyManager.positionHistory;

            Dictionary<BaseActor, Vector3Int> predicted =
                SimulateFollowerRushPlans(members, historyAfterRecord, leaderGrid);

            InvokeProcessFollowerRush(context.InputHandler);

            for (int i = 1; i < members.Count; i++)
            {
                BaseActor follower = members[i];
                Vector3Int historicalTarget =
                    i < historyAfterRecord.Count ? historyAfterRecord[i] : follower.GridPosition;

                Assert.AreEqual(predicted[follower], follower.GridPosition);

                float beforeDist = Vector3Int.Distance(followerStarts[follower], historicalTarget);
                float afterDist = Vector3Int.Distance(follower.GridPosition, historicalTarget);
                Assert.LessOrEqual(afterDist, beforeDist);
                Assert.LessOrEqual(beforeDist - afterDist, 2.001f);
            }

            AssertAllPositionsUnique(members.Select(m => m.GridPosition).ToList());
            AssertRushLeavesFreshPlayerTurn(members, context.TurnManager);
        }

        private TestFixtureContext CreateFixture(int partySize)
        {
            CreateManagersAndMap();
            PartyManager partyManager = CreatePartyManagerWithMembers(partySize);
            JRogue.Input.InputHandler inputHandler = CreateInputHandler();

            return new TestFixtureContext
            {
                InputHandler = inputHandler,
                PartyManager = partyManager,
                TurnManager = TurnManager.Instance
            };
        }

        private void CreateManagersAndMap()
        {
            GameObject mapManagerObject = new GameObject("MapManager_Test");
            _createdObjects.Add(mapManagerObject);
            MapManager mapManager = mapManagerObject.AddComponent<MapManager>();

            GameObject gridRoot = new GameObject("GridRoot_Test");
            _createdObjects.Add(gridRoot);
            gridRoot.AddComponent<Grid>();

            GameObject floorObject = new GameObject("FloorTilemap_Test");
            _createdObjects.Add(floorObject);
            floorObject.transform.SetParent(gridRoot.transform);
            Tilemap floorMap = floorObject.AddComponent<Tilemap>();
            floorObject.AddComponent<TilemapRenderer>();

            GameObject wallObject = new GameObject("WallTilemap_Test");
            _createdObjects.Add(wallObject);
            wallObject.transform.SetParent(gridRoot.transform);
            Tilemap wallMap = wallObject.AddComponent<Tilemap>();
            wallObject.AddComponent<TilemapRenderer>();

            // Wider than default play areas so west-aligned explicit tests can place leaders near x = -22 and step into walkable tiles.
            PopulateWalkableFloor(floorMap, radius: 60);

            SetPrivateField(mapManager, "floorMap", floorMap);
            SetPrivateField(mapManager, "wallMap", wallMap);

            GameObject gridManagerObject = new GameObject("GridManager_Test");
            _createdObjects.Add(gridManagerObject);
            gridManagerObject.AddComponent<GridManager>();

            GameObject turnManagerObject = new GameObject("TurnManager_Test");
            _createdObjects.Add(turnManagerObject);
            TurnManager turnManager = turnManagerObject.AddComponent<TurnManager>();
            turnManager.currentState = GameState.PLAYER_TURN;

            Assert.IsNotNull(mapManager);
            Assert.IsNotNull(GridManager.Instance);
            Assert.IsNotNull(TurnManager.Instance);
        }

        private PartyManager CreatePartyManagerWithMembers(int count)
        {
            GameObject managerObject = new GameObject("PartyManager_Test");
            _createdObjects.Add(managerObject);
            PartyManager partyManager = managerObject.AddComponent<PartyManager>();
            partyManager.partyMembers = new List<BaseActor>();
            List<IActorSeed> actorSeeds = CreateActorSeeds(count);

            for (int i = 0; i < count; i++)
            {
                GameObject actorObject = new GameObject($"InputPartyActor_{i}");
                _createdObjects.Add(actorObject);

                // Subclass satisfies BaseActor's RequireComponent(EssenceSlotManager); CharacterStats is added automatically with TestPartyActor.
                actorObject.AddComponent<TestQuietEssenceSlotManager>();

                TestPartyActor actor = actorObject.AddComponent<TestPartyActor>();

                actor.SetGridPosition(actorSeeds[i].GridPosition);
                InitializeActorRuntimeDependencies(actor);
                partyManager.partyMembers.Add(actor);
            }

            Assert.AreEqual(count, partyManager.partyMembers.Count);
            return partyManager;
        }

        private JRogue.Input.InputHandler CreateInputHandler()
        {
            GameObject inputObject = new GameObject("InputHandler_Test");
            _createdObjects.Add(inputObject);
            return inputObject.AddComponent<JRogue.Input.InputHandler>();
        }

        /// <summary>
        /// One entry per party member. Follower slot <c>i</c> must not equal the leader tile — rush uses
        /// <see cref="JRogue.Input.InputHandler"/> occupancy rules without ally stacking, so breadcrumbs use each actor's grid cell.
        /// </summary>
        private static List<Vector3Int> BuildTrailHistory(List<BaseActor> members)
        {
            var history = new List<Vector3Int>(members.Count);
            for (int i = 0; i < members.Count; i++)
                history.Add(members[i].GridPosition);

            return history;
        }

        private static void RegisterCurrentPartyOnGrid(List<BaseActor> members)
        {
            foreach (BaseActor member in members)
            {
                GridManager.Instance.RegisterActor(member.GridPosition, member);
            }
        }

        /// <summary>
        /// Mirrors <see cref="JRogue.Input.InputHandler.ProcessFollowerRush"/> planning phase: after the lift
        /// only <paramref name="leaderGrid"/> occupies the grid (followers may not jump onto it).
        /// </summary>
        private static Dictionary<BaseActor, Vector3Int> SimulateFollowerRushPlans(
            IReadOnlyList<BaseActor> party,
            IReadOnlyList<Vector3Int> history,
            Vector3Int leaderGrid,
            int maxRushDistance = 2)
        {
            var plannedMoves = new Dictionary<BaseActor, Vector3Int>();

            bool TileValid(Vector3Int tile)
            {
                if (!MapManager.Instance.IsWalkable(tile))
                    return false;
                if (tile == leaderGrid)
                    return false;
                return !plannedMoves.ContainsValue(tile);
            }

            for (int i = 1; i < party.Count; i++)
            {
                BaseActor follower = party[i];
                Vector3Int historicalTarget = i < history.Count ? history[i] : follower.GridPosition;

                Vector3Int finalTarget = follower.GridPosition;
                float dist = Vector3Int.Distance(follower.GridPosition, historicalTarget);
                if (dist <= maxRushDistance)
                    finalTarget = historicalTarget;
                else
                {
                    Vector3 direction = ((Vector3)(historicalTarget - follower.GridPosition)).normalized;
                    finalTarget = Vector3Int.RoundToInt((Vector3)follower.GridPosition + direction * maxRushDistance);
                }

                if (TileValid(finalTarget))
                {
                    plannedMoves.Add(follower, finalTarget);
                    continue;
                }

                Vector3Int bestSmartTile = follower.GridPosition;
                float bestDistToBreadcrumb = float.MaxValue;
                bool foundSpot = false;

                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        if (x == 0 && y == 0)
                            continue;
                        Vector3Int neighbor = finalTarget + new Vector3Int(x, y, 0);
                        if (Vector3Int.Distance(follower.GridPosition, neighbor) > maxRushDistance + 0.5f)
                            continue;

                        if (TileValid(neighbor))
                        {
                            float d = Vector3Int.Distance(neighbor, historicalTarget);
                            if (d < bestDistToBreadcrumb)
                            {
                                bestDistToBreadcrumb = d;
                                bestSmartTile = neighbor;
                                foundSpot = true;
                            }
                        }
                    }
                }

                plannedMoves.Add(follower, foundSpot ? bestSmartTile : follower.GridPosition);
            }

            return plannedMoves;
        }

        private void InvokeProcessFollowerRush(JRogue.Input.InputHandler inputHandler)
        {
            _processFollowerRushMethod.Invoke(inputHandler, null);
        }

        private static void PopulateWalkableFloor(Tilemap floorMap, int radius)
        {
            Tile tile = ScriptableObject.CreateInstance<Tile>();
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    floorMap.SetTile(new Vector3Int(x, y, 0), tile);
                }
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected private field '{fieldName}' to exist.");
            field.SetValue(target, value);
        }

        private static List<IActorSeed> CreateActorSeeds(int count)
        {
            var seeds = new List<IActorSeed>(count);
            for (int i = 0; i < count; i++)
            {
                IActorSeed seed = Substitute.For<IActorSeed>();
                seed.GridPosition.Returns(new Vector3Int(0, -i, 0));
                seeds.Add(seed);
            }

            return seeds;
        }

        private static void InitializeActorRuntimeDependencies(BaseActor actor)
        {
            FieldInfo mapManagerField = typeof(BaseActor).GetField("mapManager", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(mapManagerField, "Expected protected field 'mapManager' to exist on BaseActor.");
            mapManagerField.SetValue(actor, MapManager.Instance);
        }

        private static void AssertRushLeavesFreshPlayerTurn(IEnumerable<BaseActor> members, TurnManager turnManager)
        {
            // ProcessFollowerRush finishes the squad turn (EnemyTurnSequence clears acted markers) and yields a new PLAYER_TURN.
            Assert.AreEqual(GameState.PLAYER_TURN, turnManager.currentState);
            foreach (BaseActor member in members)
            {
                Assert.IsTrue(
                    turnManager.CanActorTakeAction(member.gameObject),
                    $"{member.name} should be able to act at the start of the new player turn.");
            }
        }

        private static void AssertAllPositionsUnique(IReadOnlyList<Vector3Int> positions)
        {
            Assert.AreEqual(positions.Count, positions.Distinct().Count(), "Expected unique party positions.");
        }

        private sealed class TestFixtureContext
        {
            public JRogue.Input.InputHandler InputHandler { get; set; }
            public PartyManager PartyManager { get; set; }
            public TurnManager TurnManager { get; set; }
        }

        private class TestPartyActor : BaseActor
        {
            protected override void Die()
            {
                // Not needed for unit tests.
            }
        }

        public interface IActorSeed
        {
            Vector3Int GridPosition { get; }
        }
    }
}
