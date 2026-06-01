using System.Reflection;
using JRogue.World.Generation;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.World
{
    [TestFixture]
    public sealed class DungeonTimeLogicTests
    {
        [Test]
        public void AdvancePlayerTurn_CompletesDayThenNight()
        {
            DungeonFloorDefinition floor = CreateFloor(day: 2, night: 2);
            var state = new DungeonTimeRunState();
            state.ResetForNewRun("dungeon_floor_01", 7);

            Assert.IsFalse(DungeonTimeLogic.AdvancePlayerTurn(state, floor).PhaseAdvanced);
            var afterDay = DungeonTimeLogic.AdvancePlayerTurn(state, floor);
            Assert.IsTrue(afterDay.PhaseAdvanced);
            Assert.AreEqual(DungeonTimePhase.Night, state.CurrentPhase);

            Assert.IsFalse(DungeonTimeLogic.AdvancePlayerTurn(state, floor).CycleCompleted);
            var afterNight = DungeonTimeLogic.AdvancePlayerTurn(state, floor);
            Assert.IsTrue(afterNight.CycleCompleted);
            Assert.AreEqual(1, state.ElapsedCycles);
            Assert.AreEqual(DungeonTimePhase.Day, state.CurrentPhase);
        }

        [Test]
        public void AdvancePlayerTurn_ExpiresWhenCyclesReachMaximum()
        {
            DungeonFloorDefinition floor = CreateFloor(day: 1, night: 1);
            var state = new DungeonTimeRunState();
            state.ResetForNewRun("dungeon_floor_01", 1);

            DungeonTimeLogic.AdvancePlayerTurn(state, floor);
            DungeonTimeTickResult expired = DungeonTimeLogic.AdvancePlayerTurn(state, floor);

            Assert.IsTrue(expired.TimeExpired);
            Assert.AreEqual(1, state.ElapsedCycles);
        }

        [Test]
        public void ApplyFirstVisitBudget_AddsAdditionalOnSecondFloor()
        {
            DungeonFloorDefinition floor2 = CreateFloor(day: 3, night: 4);
            SetField(floor2, "floorId", "dungeon_floor_02");
            SetField(floor2, "additionalDayNightCycles", 3);

            var state = new DungeonTimeRunState();
            state.ResetForNewRun("dungeon_floor_01", 7);

            DungeonTimeLogic.ApplyFirstVisitBudget(state, floor2, isFirstFloorInChain: false, isFirstVisit: true);

            Assert.AreEqual(10, state.MaximumCycles);
            Assert.IsTrue(state.AppliedAdditionalBudgetFloors.Contains("dungeon_floor_02"));
        }

        static DungeonFloorDefinition CreateFloor(int day, int night)
        {
            var floor = ScriptableObject.CreateInstance<DungeonFloorDefinition>();
            SetField(floor, "playerTurnsPerDay", day);
            SetField(floor, "playerTurnsPerNight", night);
            SetField(floor, "participatesInDungeonTime", true);
            return floor;
        }

        static void SetField(DungeonFloorDefinition floor, string fieldName, object value)
        {
            FieldInfo field = typeof(DungeonFloorDefinition).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            field?.SetValue(floor, value);
        }
    }
}
