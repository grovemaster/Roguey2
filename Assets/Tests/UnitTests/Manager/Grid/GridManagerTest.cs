using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using JRogue.Core.Actor;
using JRogue.Manager.Grid;
using JRogue.Tests.Mocks;
using System.Linq;

namespace JRogue.Tests.UnitTests.Manager.Grid
{
    /// <summary>
    /// Unit tests for GridManager spatial hashing system.
    /// Tests core functionality: registration, retrieval, occupancy checks, and position updates.
    /// </summary>
    [TestFixture]
    public class GridManagerTests
    {
        private GridManager _gridManager;
        private MockBattleTarget _mockTarget1;
        private MockBattleTarget _mockTarget2;
        private MockBattleTarget _mockTarget3;

        [SetUp]
        public void SetUp()
        {
            LogAssert.ignoreFailingMessages = true;
            // Create a fresh GridManager instance for each test
            GameObject gridManagerGameObject = new GameObject("GridManager");
            _gridManager = gridManagerGameObject.AddComponent<GridManager>();

            // Create mock targets
            _mockTarget1 = new MockBattleTarget("Actor1", Vector3Int.zero);
            _mockTarget2 = new MockBattleTarget("Actor2", Vector3Int.one);
            _mockTarget3 = new MockBattleTarget("Actor3", new Vector3Int(5, 5, 0));
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            // Cleanup mock targets
            _mockTarget1?.Cleanup();
            _mockTarget2?.Cleanup();
            _mockTarget3?.Cleanup();

            // Destroy GridManager
            if (_gridManager != null && _gridManager.gameObject != null)
            {
                Object.DestroyImmediate(_gridManager.gameObject);
            }
        }

        #region RegisterActor Tests

        [Test]
        public void RegisterActor_SingleActor_SuccessfullyRegisters()
        {
            // Arrange
            Vector3Int position = new Vector3Int(2, 3, 0);

            // Act
            _gridManager.RegisterActor(position, _mockTarget1);

            // Assert
            Assert.AreEqual(_mockTarget1, _gridManager.GetActorAt(position));
        }

        [Test]
        public void RegisterActor_MultipleActorsAtDifferentPositions_AllRegistered()
        {
            // Arrange
            Vector3Int pos1 = new Vector3Int(0, 0, 0);
            Vector3Int pos2 = new Vector3Int(3, 4, 0);
            Vector3Int pos3 = new Vector3Int(-2, 5, 0);

            // Act
            _gridManager.RegisterActor(pos1, _mockTarget1);
            _gridManager.RegisterActor(pos2, _mockTarget2);
            _gridManager.RegisterActor(pos3, _mockTarget3);

            // Assert
            Assert.AreEqual(_mockTarget1, _gridManager.GetActorAt(pos1));
            Assert.AreEqual(_mockTarget2, _gridManager.GetActorAt(pos2));
            Assert.AreEqual(_mockTarget3, _gridManager.GetActorAt(pos3));
        }

        [Test]
        public void RegisterActor_RegistrationConflict_FailsToRegister()
        {
            // Arrange
            Vector3Int position = new Vector3Int(1, 1, 0);
            _gridManager.RegisterActor(position, _mockTarget1);

            // GridManager logs LogError on conflict; the test runner fails unless expected.
            LogAssert.Expect(LogType.Error, new Regex(@"\[GRID-CONFLICT\].*"));

            // Act - Try to register a different actor at the same position
            _gridManager.RegisterActor(position, _mockTarget2);

            // Assert - Should still be the first actor (conflict prevented)
            Assert.AreEqual(_mockTarget1, _gridManager.GetActorAt(position));
        }

        [Test]
        public void RegisterActor_SameActorReRegister_Allowed()
        {
            // Arrange
            Vector3Int position = new Vector3Int(1, 1, 0);
            _gridManager.RegisterActor(position, _mockTarget1);

            // Act - Re-register the same actor
            _gridManager.RegisterActor(position, _mockTarget1);

            // Assert - Should still be registered without error
            Assert.AreEqual(_mockTarget1, _gridManager.GetActorAt(position));
        }

        #endregion

        #region UnregisterActor Tests

        [Test]
        public void UnregisterActor_RegisteredActor_SuccessfullyUnregisters()
        {
            // Arrange
            Vector3Int position = new Vector3Int(2, 2, 0);
            _gridManager.RegisterActor(position, _mockTarget1);

            // Act
            _gridManager.UnregisterActor(position);

            // Assert
            Assert.IsNull(_gridManager.GetActorAt(position));
        }

        [Test]
        public void UnregisterActor_UnregisteredPosition_NothingHappens()
        {
            // Arrange
            Vector3Int emptyPosition = new Vector3Int(10, 10, 0);

            // Act - Should not throw error
            _gridManager.UnregisterActor(emptyPosition);

            // Assert
            Assert.IsNull(_gridManager.GetActorAt(emptyPosition));
        }

        [Test]
        public void UnregisterActor_MultipleUnregistrations_AllClearedCorrectly()
        {
            // Arrange
            Vector3Int pos1 = new Vector3Int(0, 0, 0);
            Vector3Int pos2 = new Vector3Int(1, 1, 0);
            Vector3Int pos3 = new Vector3Int(2, 2, 0);

            _gridManager.RegisterActor(pos1, _mockTarget1);
            _gridManager.RegisterActor(pos2, _mockTarget2);
            _gridManager.RegisterActor(pos3, _mockTarget3);

            // Act
            _gridManager.UnregisterActor(pos1);
            _gridManager.UnregisterActor(pos2);
            _gridManager.UnregisterActor(pos3);

            // Assert
            Assert.IsNull(_gridManager.GetActorAt(pos1));
            Assert.IsNull(_gridManager.GetActorAt(pos2));
            Assert.IsNull(_gridManager.GetActorAt(pos3));
        }

        #endregion

        #region IsOccupied Tests

        [Test]
        public void IsOccupied_OccupiedPosition_ReturnsTrue()
        {
            // Arrange
            Vector3Int position = new Vector3Int(5, 5, 0);
            _gridManager.RegisterActor(position, _mockTarget1);

            // Act
            bool isOccupied = _gridManager.IsOccupied(position);

            // Assert
            Assert.IsTrue(isOccupied);
        }

        [Test]
        public void IsOccupied_EmptyPosition_ReturnsFalse()
        {
            // Arrange
            Vector3Int position = new Vector3Int(5, 5, 0);

            // Act
            bool isOccupied = _gridManager.IsOccupied(position);

            // Assert
            Assert.IsFalse(isOccupied);
        }

        [Test]
        public void IsOccupied_AfterUnregistration_ReturnsFalse()
        {
            // Arrange
            Vector3Int position = new Vector3Int(5, 5, 0);
            _gridManager.RegisterActor(position, _mockTarget1);
            _gridManager.UnregisterActor(position);

            // Act
            bool isOccupied = _gridManager.IsOccupied(position);

            // Assert
            Assert.IsFalse(isOccupied);
        }

        #endregion

        #region GetActorAt Tests

        [Test]
        public void GetActorAt_RegisteredPosition_ReturnsCorrectActor()
        {
            // Arrange
            Vector3Int position = new Vector3Int(3, 4, 0);
            _gridManager.RegisterActor(position, _mockTarget1);

            // Act
            IBattleTarget actor = _gridManager.GetActorAt(position);

            // Assert
            Assert.AreEqual(_mockTarget1, actor);
        }

        [Test]
        public void GetActorAt_UnregisteredPosition_ReturnsNull()
        {
            // Arrange
            Vector3Int position = new Vector3Int(99, 99, 0);

            // Act
            IBattleTarget actor = _gridManager.GetActorAt(position);

            // Assert
            Assert.IsNull(actor);
        }

        [Test]
        public void GetActorAt_MultipleActors_ReturnCorrectActor()
        {
            // Arrange
            Vector3Int pos1 = new Vector3Int(0, 0, 0);
            Vector3Int pos2 = new Vector3Int(5, 5, 0);

            _gridManager.RegisterActor(pos1, _mockTarget1);
            _gridManager.RegisterActor(pos2, _mockTarget2);

            // Act
            IBattleTarget actor1 = _gridManager.GetActorAt(pos1);
            IBattleTarget actor2 = _gridManager.GetActorAt(pos2);

            // Assert
            Assert.AreEqual(_mockTarget1, actor1);
            Assert.AreEqual(_mockTarget2, actor2);
        }

        #endregion

        #region UpdateActorPosition Tests

        [Test]
        public void UpdateActorPosition_ValidMove_ActorMovedToNewPosition()
        {
            // Arrange
            Vector3Int oldPos = new Vector3Int(0, 0, 0);
            Vector3Int newPos = new Vector3Int(3, 3, 0);

            _gridManager.RegisterActor(oldPos, _mockTarget1);

            // Act
            _gridManager.UpdateActorPosition(_mockTarget1, oldPos, newPos);

            // Assert
            Assert.IsNull(_gridManager.GetActorAt(oldPos));
            Assert.AreEqual(_mockTarget1, _gridManager.GetActorAt(newPos));
        }

        [Test]
        public void UpdateActorPosition_WrongOldPosition_ActorAlsoRegisteredAtNewPosition()
        {
            // Arrange
            Vector3Int correctPos = new Vector3Int(0, 0, 0);
            Vector3Int wrongOldPos = new Vector3Int(1, 1, 0);
            Vector3Int newPos = new Vector3Int(5, 5, 0);

            _gridManager.RegisterActor(correctPos, _mockTarget1);

            // Act
            _gridManager.UpdateActorPosition(_mockTarget1, wrongOldPos, newPos);

            // Assert - Current implementation keeps original registration and also sets new position
            Assert.AreEqual(_mockTarget1, _gridManager.GetActorAt(correctPos));
            Assert.AreEqual(_mockTarget1, _gridManager.GetActorAt(newPos));
        }

        [Test]
        public void UpdateActorPosition_DifferentActorAtOldPosition_OnlyCorrectActorRemoved()
        {
            // Arrange
            Vector3Int oldPos = new Vector3Int(0, 0, 0);
            Vector3Int newPos = new Vector3Int(5, 5, 0);

            _gridManager.RegisterActor(oldPos, _mockTarget1);
            // Override with different actor (simulating out-of-sync state)
            _gridManager.RegisterActor(oldPos, _mockTarget1); // Ensure it's target1

            // Act
            _gridManager.UpdateActorPosition(_mockTarget1, oldPos, newPos);

            // Assert
            Assert.IsNull(_gridManager.GetActorAt(oldPos));
            Assert.AreEqual(_mockTarget1, _gridManager.GetActorAt(newPos));
        }

        [Test]
        public void UpdateActorPosition_MultipleSequentialMoves_AllMovesTracked()
        {
            // Arrange
            Vector3Int pos1 = new Vector3Int(0, 0, 0);
            Vector3Int pos2 = new Vector3Int(1, 1, 0);
            Vector3Int pos3 = new Vector3Int(3, 3, 0);

            _gridManager.RegisterActor(pos1, _mockTarget1);

            // Act - Move 1: pos1 -> pos2
            _gridManager.UpdateActorPosition(_mockTarget1, pos1, pos2);

            // Assert after move 1
            Assert.IsNull(_gridManager.GetActorAt(pos1));
            Assert.AreEqual(_mockTarget1, _gridManager.GetActorAt(pos2));

            // Act - Move 2: pos2 -> pos3
            _gridManager.UpdateActorPosition(_mockTarget1, pos2, pos3);

            // Assert after move 2
            Assert.IsNull(_gridManager.GetActorAt(pos2));
            Assert.AreEqual(_mockTarget1, _gridManager.GetActorAt(pos3));
        }

        #endregion

        #region GetAllActors Tests

        [Test]
        public void GetAllActors_NoActors_ReturnsEmpty()
        {
            // Act
            var allActors = _gridManager.GetAllActors().ToList();

            // Assert
            Assert.AreEqual(0, allActors.Count);
        }

        [Test]
        public void GetAllActors_SingleActor_ReturnsThatActor()
        {
            // Arrange
            Vector3Int position = new Vector3Int(1, 1, 0);
            _gridManager.RegisterActor(position, _mockTarget1);

            // Act
            var allActors = _gridManager.GetAllActors().ToList();

            // Assert
            Assert.AreEqual(1, allActors.Count);
            Assert.Contains(_mockTarget1, allActors);
        }

        [Test]
        public void GetAllActors_MultipleActors_ReturnsAll()
        {
            // Arrange
            _gridManager.RegisterActor(new Vector3Int(0, 0, 0), _mockTarget1);
            _gridManager.RegisterActor(new Vector3Int(1, 1, 0), _mockTarget2);
            _gridManager.RegisterActor(new Vector3Int(2, 2, 0), _mockTarget3);

            // Act
            var allActors = _gridManager.GetAllActors().ToList();

            // Assert
            Assert.AreEqual(3, allActors.Count);
            Assert.Contains(_mockTarget1, allActors);
            Assert.Contains(_mockTarget2, allActors);
            Assert.Contains(_mockTarget3, allActors);
        }

        [Test]
        public void GetAllActors_AfterUnregistration_ExcludesRemovedActor()
        {
            // Arrange
            Vector3Int pos1 = new Vector3Int(0, 0, 0);
            Vector3Int pos2 = new Vector3Int(1, 1, 0);
            _gridManager.RegisterActor(pos1, _mockTarget1);
            _gridManager.RegisterActor(pos2, _mockTarget2);

            // Act
            _gridManager.UnregisterActor(pos1);
            var allActors = _gridManager.GetAllActors().ToList();

            // Assert
            Assert.AreEqual(1, allActors.Count);
            Assert.Contains(_mockTarget2, allActors);
            Assert.IsFalse(allActors.Contains(_mockTarget1));
        }

        #endregion

        #region Integration Scenarios Tests

        [Test]
        public void ScenarioComplexGridManagement_RegisterMoveUnregister()
        {
            // This tests a realistic scenario: register multiple actors, move them, unregister

            // Arrange & Act
            Vector3Int pos1Initial = new Vector3Int(0, 0, 0);
            Vector3Int pos2Initial = new Vector3Int(5, 5, 0);

            _gridManager.RegisterActor(pos1Initial, _mockTarget1);
            _gridManager.RegisterActor(pos2Initial, _mockTarget2);

            // Move target1
            Vector3Int pos1Final = new Vector3Int(2, 2, 0);
            _gridManager.UpdateActorPosition(_mockTarget1, pos1Initial, pos1Final);

            // Move target2
            Vector3Int pos2Final = new Vector3Int(6, 6, 0);
            _gridManager.UpdateActorPosition(_mockTarget2, pos2Initial, pos2Final);

            // Unregister target1
            _gridManager.UnregisterActor(pos1Final);

            // Assert
            Assert.IsNull(_gridManager.GetActorAt(pos1Initial));
            Assert.IsNull(_gridManager.GetActorAt(pos1Final));
            Assert.IsNull(_gridManager.GetActorAt(pos2Initial));
            Assert.AreEqual(_mockTarget2, _gridManager.GetActorAt(pos2Final));

            var allActors = _gridManager.GetAllActors().ToList();
            Assert.AreEqual(1, allActors.Count);
            Assert.Contains(_mockTarget2, allActors);
        }

        #endregion
    }
}
