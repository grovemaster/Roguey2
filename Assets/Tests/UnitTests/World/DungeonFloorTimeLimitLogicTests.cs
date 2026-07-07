using System.Reflection;
using JRogue.World.Generation;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.World
{
    [TestFixture]
    public sealed class DungeonFloorTimeLimitLogicTests
    {
        [Test]
        public void IsFloorExpiredForPortal_WhenElapsedReachesLimit()
        {
            DungeonFloorDefinition floor1 = CreateFloor("dungeon_floor_01", floorLimit: 4);

            Assert.IsFalse(DungeonFloorTimeLimitLogic.IsFloorExpiredForPortal(floor1, 3));
            Assert.IsTrue(DungeonFloorTimeLimitLogic.IsFloorExpiredForPortal(floor1, 4));
        }

        [Test]
        public void IsFloorExpiredForPortal_LegacyFloorWithZeroLimit_NeverBlocks()
        {
            DungeonFloorDefinition legacy = CreateFloor("dungeon_floor_01", floorLimit: 0);

            Assert.IsFalse(DungeonFloorTimeLimitLogic.IsFloorExpiredForPortal(legacy, 99));
        }

        [Test]
        public void AdvancePlayerTurn_ExpiresOnActiveFloorLimit_NotGlobalMaximum()
        {
            DungeonFloorDefinition floor2 = CreateFloor("dungeon_floor_02", floorLimit: 6);
            var state = new DungeonTimeRunState();
            state.ResetForNewRun("dungeon_floor_01", 4);

            for (int cycle = 0; cycle < 5; cycle++)
            {
                DungeonTimeLogic.AdvancePlayerTurn(state, floor2);
                DungeonTimeTickResult tick = DungeonTimeLogic.AdvancePlayerTurn(state, floor2);
                Assert.IsFalse(tick.TimeExpired, $"Should not expire at cycle {state.ElapsedCycles}");
            }

            Assert.AreEqual(5, state.ElapsedCycles);

            DungeonTimeLogic.AdvancePlayerTurn(state, floor2);
            DungeonTimeTickResult expired = DungeonTimeLogic.AdvancePlayerTurn(state, floor2);
            Assert.IsTrue(expired.TimeExpired);
            Assert.AreEqual(6, state.ElapsedCycles);
        }

        [Test]
        public void AdvancePlayerTurn_Floor1LimitFour_ExpiresOnFloor1()
        {
            DungeonFloorDefinition floor1 = CreateFloor("dungeon_floor_01", floorLimit: 4);
            var state = new DungeonTimeRunState();
            state.ResetForNewRun("dungeon_floor_01", 4);

            for (int cycle = 0; cycle < 3; cycle++)
            {
                DungeonTimeLogic.AdvancePlayerTurn(state, floor1);
                Assert.IsFalse(DungeonTimeLogic.AdvancePlayerTurn(state, floor1).TimeExpired);
            }

            DungeonTimeLogic.AdvancePlayerTurn(state, floor1);
            Assert.IsTrue(DungeonTimeLogic.AdvancePlayerTurn(state, floor1).TimeExpired);
            Assert.AreEqual(4, state.ElapsedCycles);
        }

        [Test]
        public void ProductionFloorAssets_HavePerFloorLimits()
        {
            var floor1 = Resources.Load<DungeonFloorDefinition>("Dungeon/Floor_prod_dungeon_floor_01");
            var floor2 = Resources.Load<DungeonFloorDefinition>("Dungeon/Floor_prod_dungeon_floor_02");
            Assert.IsNotNull(floor1);
            Assert.IsNotNull(floor2);
            Assert.AreEqual(4, floor1.FloorDayNightCycleLimit);
            Assert.AreEqual(6, floor2.FloorDayNightCycleLimit);
            Assert.AreEqual(0, floor2.AdditionalDayNightCycles);
        }

        static DungeonFloorDefinition CreateFloor(string floorId, int floorLimit)
        {
            var floor = ScriptableObject.CreateInstance<DungeonFloorDefinition>();
            SetField(floor, "floorId", floorId);
            SetField(floor, "participatesInDungeonTime", true);
            SetField(floor, "playerTurnsPerDay", 1);
            SetField(floor, "playerTurnsPerNight", 1);
            SetField(floor, "floorDayNightCycleLimit", floorLimit);
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
