using JRogue.World.Generation;
using JRogue.World.Generation.Phases;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.World
{
    public sealed class DungeonFloor1Phase5Tests
    {
        [Test]
        public void ParseFloorNumber_ReadsTrailingIndex()
        {
            Assert.AreEqual(1, DungeonRunState.ParseFloorNumber("dungeon_floor_01"));
            Assert.AreEqual(2, DungeonRunState.ParseFloorNumber("dungeon_floor_02"));
            Assert.AreEqual(0, DungeonRunState.ParseFloorNumber("dimension_square"));
        }

        [Test]
        public void PortalTrapBuffer_ExcludesCellsWithinChebyshevFive()
        {
            var centers = new[] { new Vector3Int(25, 79, 0) };

            Assert.IsTrue(PopulationPlacementUtility.IsWithinChebyshevDistanceOfAny(
                new Vector3Int(25, 79, 0),
                centers,
                PopulationPlacementUtility.DefaultPortalTrapBufferChebyshev));
            Assert.IsTrue(PopulationPlacementUtility.IsWithinChebyshevDistanceOfAny(
                new Vector3Int(30, 74, 0),
                centers,
                PopulationPlacementUtility.DefaultPortalTrapBufferChebyshev));
            Assert.IsFalse(PopulationPlacementUtility.IsWithinChebyshevDistanceOfAny(
                new Vector3Int(31, 73, 0),
                centers,
                PopulationPlacementUtility.DefaultPortalTrapBufferChebyshev));
        }
    }
}
