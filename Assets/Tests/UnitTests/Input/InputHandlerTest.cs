using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using JRogue.Actors;
using JRogue.Manager.Grid;
using JRogue.Manager.Map;
using JRogue.Manager.Party;
using JRogue.Input;
using JRogue.Manager.Turn;
using JRogue.Tests.UnitTests.MockMonoBehavior;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace JRogue.Tests.UnitTests.Input
{
    [TestFixture]
    public class InputHandlerTest
    {
        private readonly List<GameObject> _createdObjects = new List<GameObject>();
        private MethodInfo _processFollowerRushMethod;
        private static object s_capturedPerformedCallbackContext;

        [SetUp]
        public void SetUp()
        {
            LogAssert.ignoreFailingMessages = true;
            _processFollowerRushMethod = typeof(JRogue.Input.InputHandler)
                .GetMethod("ProcessFollowerRush", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(_processFollowerRushMethod, "Expected private method ProcessFollowerRush to exist.");
            s_capturedPerformedCallbackContext = null;
        }

        [TearDown]
        public void TearDown()
        {
            s_capturedPerformedCallbackContext = null;
            LogAssert.ignoreFailingMessages = false;
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
            context.PartyManager.positionHistory = InputTestSceneBuilder.BuildTrailHistory(context.PartyManager.partyMembers);
            InputTestSceneBuilder.RegisterCurrentPartyOnGrid(context.PartyManager.partyMembers);

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
            InputTestSceneBuilder.RegisterCurrentPartyOnGrid(members);

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
            InputTestSceneBuilder.RegisterCurrentPartyOnGrid(context.PartyManager.partyMembers);

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

            InputTestSceneBuilder.RegisterCurrentPartyOnGrid(members);
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
            InputTestSceneBuilder.RegisterCurrentPartyOnGrid(members);

            context.PartyManager.positionHistory = new List<Vector3Int>
            {
                new Vector3Int(0, 0, 0),
                new Vector3Int(0, -1, 0),
                new Vector3Int(0, -2, 0)
            };
            List<Vector3Int> historyBefore = new List<Vector3Int>(context.PartyManager.positionHistory);

            // Mimics OnMove branch where the leader attacked/bumped and position did not change.
            context.PartyManager.RecordNewLeaderPosition(members[0].GridPosition);
            InvokeProcessFollowerRush(context.InputHandler);

            CollectionAssert.AreEqual(historyBefore, context.PartyManager.positionHistory);
            Assert.AreEqual(new Vector3Int(0, -1, 0), members[1].GridPosition);
            Assert.AreEqual(new Vector3Int(0, -2, 0), members[2].GridPosition);
        }

        /// <summary>
        /// Mirrors formation rush planning (<see cref="JRogue.Input.PlayerCommandProcessor"/>) while the spatial hash only holds the leader
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
            InputTestSceneBuilder.RegisterCurrentPartyOnGrid(members);

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
            InputTestSceneBuilder.RegisterCurrentPartyOnGrid(members);
            partyManager.SnapHistoryToCurrentPositions();

            Vector3Int leaderBeforeHist0 = partyManager.positionHistory[0];
            Vector3Int moveAxis = Vector3Int.left;
            Assert.IsTrue(members[0].TryMove(moveAxis), "Leader advance should succeed on blank floor.");

            Vector3Int leaderExpected = members[0].GridPosition;
            Assert.AreEqual(leaderBeforeHist0 + moveAxis, leaderExpected);

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

        [Test]
        public void ProcessFollowerRush_FirstFollowerAlreadyActed_KeepsTileSoSecondFollowerDoesNotTakeIt()
        {
            TestFixtureContext context = CreateFixture(3);
            List<BaseActor> members = context.PartyManager.partyMembers;
            PartyManager partyManager = context.PartyManager;
            BaseActor leader = members[0];
            BaseActor follower1 = members[1];
            BaseActor follower2 = members[2];

            leader.SetGridPosition(new Vector3Int(0, 0, 0));
            follower1.SetGridPosition(new Vector3Int(1, 0, 0));
            follower2.SetGridPosition(new Vector3Int(10, 0, 0));
            partyManager.positionHistory = new List<Vector3Int>
            {
                new Vector3Int(0, 0, 0),
                new Vector3Int(1, 0, 0),
                new Vector3Int(1, 0, 0)
            };
            InputTestSceneBuilder.RegisterCurrentPartyOnGrid(members);

            context.TurnManager.OnPlayerActionComplete(follower1.gameObject);
            Assert.IsFalse(context.TurnManager.CanActorTakeAction(follower1.gameObject));

            Vector3Int follower1Before = follower1.GridPosition;
            InvokeProcessFollowerRush(context.InputHandler);

            Assert.AreEqual(follower1Before, follower1.GridPosition);
            Assert.AreNotEqual(follower1Before, follower2.GridPosition, "Second follower should move toward the slot.");
            Assert.AreNotEqual(follower2.GridPosition, follower1Before,
                "Acted follower should remain registered on the breadcrumb tile so the next follower cannot occupy it.");
            Assert.AreEqual(follower1, GridManager.Instance.GetActorAt(follower1.GridPosition));
            AssertAllPositionsUnique(members.Select(m => m.GridPosition).ToList());
            AssertRushLeavesFreshPlayerTurn(members, context.TurnManager);
        }

        [Test]
        public void OnToggleFormation_Performed_TurnsOffWhenFormationWasActive()
        {
            TestFixtureContext context = CreateFixture(2);
            Assert.IsTrue(context.PartyManager.IsFormationActive, "PartyManager defaults formation to active in tests.");

            using (CapturedPerformedContextSession session = CapturedPerformedContextSession.Create("G"))
            {
                MethodInfo onToggle = typeof(JRogue.Input.InputHandler).GetMethod(
                    "OnToggleFormation",
                    BindingFlags.Instance | BindingFlags.Public);
                Assert.IsNotNull(onToggle);
                onToggle.Invoke(context.InputHandler, new[] { session.BoxedContext });
            }

            Assert.IsFalse(context.PartyManager.IsFormationActive);
        }

        [Test]
        public void OnToggleFormation_Performed_TurnsOnWhenInactiveAndLeaderCanAct_SnapsHistory()
        {
            TestFixtureContext context = CreateFixture(3);
            List<BaseActor> members = context.PartyManager.partyMembers;
            PartyManager partyManager = context.PartyManager;

            members[0].SetGridPosition(new Vector3Int(0, 0, 0));
            members[1].SetGridPosition(new Vector3Int(0, -2, 0));
            members[2].SetGridPosition(new Vector3Int(2, -1, 0));
            InputTestSceneBuilder.RegisterCurrentPartyOnGrid(members);

            InputTestSceneBuilder.SetPrivateField(partyManager, "isFormationActive", false);
            partyManager.positionHistory = new List<Vector3Int>
            {
                new Vector3Int(99, 99, 0),
                new Vector3Int(98, 98, 0),
                new Vector3Int(97, 97, 0)
            };

            using (CapturedPerformedContextSession session = CapturedPerformedContextSession.Create("H"))
            {
                MethodInfo onToggle = typeof(JRogue.Input.InputHandler).GetMethod(
                    "OnToggleFormation",
                    BindingFlags.Instance | BindingFlags.Public);
                Assert.IsNotNull(onToggle);
                onToggle.Invoke(context.InputHandler, new[] { session.BoxedContext });
            }

            Assert.IsTrue(partyManager.IsFormationActive);
            Assert.AreEqual(3, partyManager.positionHistory.Count);
            Assert.AreEqual(members[0].GridPosition, partyManager.positionHistory[0]);
            Assert.AreEqual(members[1].GridPosition, partyManager.positionHistory[1]);
            Assert.AreEqual(members[2].GridPosition, partyManager.positionHistory[2]);
        }

        [Test]
        public void OnToggleFormation_Performed_LeaderActed_LeavesFormationOff()
        {
            TestFixtureContext context = CreateFixture(2);
            PartyManager partyManager = context.PartyManager;
            BaseActor leader = partyManager.partyMembers[0];

            InputTestSceneBuilder.SetPrivateField(partyManager, "isFormationActive", false);
            context.TurnManager.OnPlayerActionComplete(leader.gameObject);
            Assert.IsFalse(context.TurnManager.CanActorTakeAction(leader.gameObject));

            LogAssert.Expect(LogType.Warning, new Regex(@"\[FORMATION\].*"));

            using (CapturedPerformedContextSession session = CapturedPerformedContextSession.Create("J"))
            {
                MethodInfo onToggle = typeof(JRogue.Input.InputHandler).GetMethod(
                    "OnToggleFormation",
                    BindingFlags.Instance | BindingFlags.Public);
                Assert.IsNotNull(onToggle);
                onToggle.Invoke(context.InputHandler, new[] { session.BoxedContext });
            }

            Assert.IsFalse(partyManager.IsFormationActive);
        }

        [Test]
        public void OnAbilityPerformed_LeaderActed_EmitsWarningAndDoesNothing()
        {
            // Party must have more than one member: with a solo party, OnPlayerActionComplete(leader) finishes
            // the squad immediately and EnemyTurnSequence clears acted state before this assertion runs.
            TestFixtureContext context = CreateFixture(2);
            List<BaseActor> members = context.PartyManager.partyMembers;
            BaseActor leader = members[0];
            BaseActor follower = members[1];

            leader.SetGridPosition(new Vector3Int(0, 0, 0));
            follower.SetGridPosition(new Vector3Int(0, -2, 0));
            InputTestSceneBuilder.RegisterCurrentPartyOnGrid(members);

            context.TurnManager.OnPlayerActionComplete(leader.gameObject);
            Assert.IsFalse(context.TurnManager.CanActorTakeAction(leader.gameObject));
            Assert.IsTrue(context.TurnManager.CanActorTakeAction(follower.gameObject));

            LogAssert.Expect(LogType.Warning, new Regex(@"\[INPUT\].*"));

            MethodInfo onAbility = typeof(JRogue.Input.InputHandler).GetMethod(
                "OnAbilityPerformed",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(onAbility);

            using (CapturedPerformedContextSession session = CapturedPerformedContextSession.Create("Digit1"))
            {
                onAbility.Invoke(context.InputHandler, new object[] { session.BoxedContext, false, false });
            }

            Assert.IsFalse(context.TurnManager.CanActorTakeAction(leader.gameObject));
            Assert.IsTrue(context.TurnManager.CanActorTakeAction(follower.gameObject));
        }

        /// <summary>
        /// Duplicates <see cref="JRogue.Input.PlayerCommandProcessor.ProcessFollowerRush"/> follower planning versus leader lock and walkable tiles;
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
            InputTestSceneBuilder.RegisterCurrentPartyOnGrid(members);
            partyManager.SnapHistoryToCurrentPositions();

            Assert.IsTrue(members[0].TryMove(Vector3Int.down));
            Vector3Int leaderGrid = members[0].GridPosition;

            var followerStarts = new Dictionary<BaseActor, Vector3Int>();
            for (int i = 1; i < members.Count; i++)
                followerStarts[members[i]] = members[i].GridPosition;

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
            InputTestSceneBuilder.RegisterCurrentPartyOnGrid(members);
            partyManager.SnapHistoryToCurrentPositions();

            Assert.IsTrue(members[0].TryMove(Vector3Int.right));
            Vector3Int leaderGrid = members[0].GridPosition;

            var followerStarts = new Dictionary<BaseActor, Vector3Int>();
            for (int i = 1; i < members.Count; i++)
                followerStarts[members[i]] = members[i].GridPosition;

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

        [Test]
        public void TryApplyRecordedCommand_MoveGrid_WhenEnemyTurn_ReturnsFalse()
        {
            TestFixtureContext context = CreateFixture(2);
            context.TurnManager.currentState = GameState.ENEMY_TURN;

            Assert.IsFalse(context.InputHandler.TryApplyRecordedCommand(PlayerCommand.MoveGrid(Vector3Int.right)));
        }

        [Test]
        public void TryApplyRecordedCommand_SwapPartyMember_WhenEnemyTurn_ReordersControlledActor()
        {
            TestFixtureContext context = CreateFixture(2);
            context.TurnManager.currentState = GameState.ENEMY_TURN;

            BaseActor first = context.PartyManager.partyMembers[0];
            BaseActor second = context.PartyManager.partyMembers[1];

            Assert.IsTrue(context.InputHandler.TryApplyRecordedCommand(PlayerCommand.SwapPartyMember(1)));
            Assert.AreSame(second, context.PartyManager.partyMembers[0]);
            Assert.AreSame(first, context.PartyManager.partyMembers[1]);
        }

        private TestFixtureContext CreateFixture(int partySize)
        {
            InputTestSceneBuilder.SetupMapAndManagers(_createdObjects);
            PartyManager partyManager = InputTestSceneBuilder.CreatePartyWithTestActors(partySize, _createdObjects);
            JRogue.Input.InputHandler inputHandler = CreateInputHandler();

            return new TestFixtureContext
            {
                InputHandler = inputHandler,
                PartyManager = partyManager,
                TurnManager = TurnManager.Instance
            };
        }

        private JRogue.Input.InputHandler CreateInputHandler()
        {
            GameObject inputObject = new GameObject("InputHandler_Test");
            _createdObjects.Add(inputObject);
            return inputObject.AddComponent<JRogue.Input.InputHandler>();
        }

        /// <summary>
        /// Mirrors follower rush planning (<see cref="JRogue.Input.PlayerCommandProcessor.ProcessFollowerRush"/>) after the lift
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

        private static Assembly ResolveUnityInputSystemAssembly()
        {
            return AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "Unity.InputSystem");
        }

        /// <summary>
        /// Maps a <see cref="UnityEngine.InputSystem.Key"/> enum member name to the control <c>name</c> segment
        /// used on <see cref="UnityEngine.InputSystem.Keyboard"/> (e.g. <c>Key.G</c> → <c>g</c>, <c>Key.Digit1</c> → <c>1</c>).
        /// </summary>
        private static string KeyEnumNameToKeyboardControlSegment(string keyEnumName)
        {
            if (string.IsNullOrEmpty(keyEnumName))
                throw new ArgumentException("Key enum name is required.", nameof(keyEnumName));

            if (keyEnumName.StartsWith("Digit", StringComparison.Ordinal) && keyEnumName.Length >= 6)
                return keyEnumName.Substring(5);

            if (keyEnumName.Length == 1)
                return keyEnumName.ToLowerInvariant();

            return keyEnumName.ToLowerInvariant();
        }

        /// <summary>
        /// Builds a real <see cref="UnityEngine.InputSystem.InputAction.CallbackContext"/> in the Performed phase
        /// without referencing Unity.InputSystem at compile time (tests assembly does not reference that package).
        /// </summary>
        private sealed class CapturedPerformedContextSession : IDisposable
        {
            private readonly Assembly _asm;
            private readonly object _keyboardDevice;
            private readonly object _inputAction;
            private readonly EventInfo _performedEvent;
            private readonly Delegate _performedDelegate;

            public object BoxedContext { get; }

            private CapturedPerformedContextSession(
                Assembly asm,
                object keyboardDevice,
                object inputAction,
                EventInfo performedEvent,
                Delegate performedDelegate,
                object boxedContext)
            {
                _asm = asm;
                _keyboardDevice = keyboardDevice;
                _inputAction = inputAction;
                _performedEvent = performedEvent;
                _performedDelegate = performedDelegate;
                BoxedContext = boxedContext;
            }

            public static CapturedPerformedContextSession Create(string keyEnumName)
            {
                Assembly asm = ResolveUnityInputSystemAssembly();
                Assert.IsNotNull(asm, "Unity.InputSystem must be loaded for InputHandler callback tests.");

                Type inputSystemType = asm.GetType("UnityEngine.InputSystem.InputSystem");
                MethodInfo addDevice = inputSystemType.GetMethod(
                    "AddDevice",
                    BindingFlags.Public | BindingFlags.Static,
                    binder: null,
                    types: new[] { typeof(string), typeof(string), typeof(string) },
                    modifiers: null);
                object keyboard = addDevice.Invoke(null, new object[] { "Keyboard", null, null });
                Assert.IsNotNull(keyboard);

                // Bind to THIS keyboard instance. "<Keyboard>/g" can resolve to a different device than the one we queue state to.
                string devicePath = (string)keyboard.GetType().GetProperty("path").GetValue(keyboard, null);
                string controlSegment = KeyEnumNameToKeyboardControlSegment(keyEnumName);
                string bindingPath = $"{devicePath}/{controlSegment}";

                Type actionType = asm.GetType("UnityEngine.InputSystem.InputAction");
                Type actionTypeEnum = asm.GetType("UnityEngine.InputSystem.InputActionType");
                object button = Enum.Parse(actionTypeEnum, "Button");
                // InputAction(string name, InputActionType type, string binding, string interactions, string processors, string expectedControlType)
                object action = Activator.CreateInstance(
                    actionType,
                    new object[] { "SynthPerf", button, bindingPath, null, null, null });
                Assert.IsNotNull(action);

                Type callbackContextType = asm.GetType("UnityEngine.InputSystem.InputAction+CallbackContext");
                Type performedDelegateType = typeof(Action<>).MakeGenericType(callbackContextType);
                FieldInfo captureField = typeof(InputHandlerTest).GetField(
                    nameof(s_capturedPerformedCallbackContext),
                    BindingFlags.Static | BindingFlags.NonPublic);
                ParameterExpression p = Expression.Parameter(callbackContextType, "ctx");
                BinaryExpression assign = Expression.Assign(
                    Expression.Field(null, captureField),
                    Expression.Convert(p, typeof(object)));
                LambdaExpression lambda = Expression.Lambda(performedDelegateType, assign, p);
                Delegate del = lambda.Compile();

                EventInfo performed = actionType.GetEvent("performed");
                performed.AddEventHandler(action, del);

                actionType.GetMethod("Enable", Type.EmptyTypes).Invoke(action, null);

                Type keyType = asm.GetType("UnityEngine.InputSystem.Key");
                object keyValue = Enum.Parse(keyType, keyEnumName);
                Array keyArray = Array.CreateInstance(keyType, 1);
                keyArray.SetValue(keyValue, 0);

                Type keyboardStateType = asm.GetType("UnityEngine.InputSystem.LowLevel.KeyboardState");
                object state = Activator.CreateInstance(keyboardStateType, new object[] { false, keyArray });

                MethodInfo queueTemplate = inputSystemType
                    .GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Single(m =>
                        m.Name == "QueueStateEvent" &&
                        m.IsGenericMethodDefinition &&
                        m.GetParameters().Length == 3 &&
                        m.GetParameters()[0].ParameterType.FullName == "UnityEngine.InputSystem.InputDevice");
                MethodInfo queue = queueTemplate.MakeGenericMethod(keyboardStateType);
                queue.Invoke(null, new[] { keyboard, state, -1.0 });

                MethodInfo update = inputSystemType.GetMethod(
                    "Update",
                    BindingFlags.Public | BindingFlags.Static,
                    binder: null,
                    types: Type.EmptyTypes,
                    modifiers: null);
                for (int i = 0; i < 4; i++)
                {
                    update.Invoke(null, null);
                }

                object captured = s_capturedPerformedCallbackContext;
                Assert.IsNotNull(captured, "Expected a performed InputAction callback to capture CallbackContext.");

                object performedPhase = Enum.Parse(asm.GetType("UnityEngine.InputSystem.InputActionPhase"), "Performed");
                object phase = callbackContextType.GetProperty("phase").GetValue(captured, null);
                Assert.AreEqual(performedPhase, phase, "Synthetic input should leave the action in Performed phase.");

                return new CapturedPerformedContextSession(asm, keyboard, action, performed, del, captured);
            }

            public void Dispose()
            {
                try
                {
                    _performedEvent.RemoveEventHandler(_inputAction, _performedDelegate);
                    _inputAction.GetType().GetMethod("Disable", Type.EmptyTypes)?.Invoke(_inputAction, null);
                    _inputAction.GetType().GetMethod("Dispose", Type.EmptyTypes)?.Invoke(_inputAction, null);

                    Type inputSystemType = _asm.GetType("UnityEngine.InputSystem.InputSystem");
                    Type deviceType = _asm.GetType("UnityEngine.InputSystem.InputDevice");
                    MethodInfo remove = inputSystemType.GetMethod("RemoveDevice", new[] { deviceType });
                    remove?.Invoke(null, new[] { _keyboardDevice });
                }
                finally
                {
                    s_capturedPerformedCallbackContext = null;
                }
            }
        }

        private sealed class TestFixtureContext
        {
            public JRogue.Input.InputHandler InputHandler { get; set; }
            public PartyManager PartyManager { get; set; }
            public TurnManager TurnManager { get; set; }
        }
    }
}
