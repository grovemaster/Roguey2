using System.Collections.Generic;
using JRogue.World.Generation.Zones;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.World
{
    [TestFixture]
    public sealed class ZoneGlowFloorGapFillLogicTests
    {
        [Test]
        public void NeedsGlowFill_WhenReceivedBelowThreshold_ReturnsTrue()
        {
            Assert.IsTrue(ZoneGlowFloorGapFillLogic.NeedsGlowFill(receivedLight: 0, minReceivedLight: 1));
            Assert.IsFalse(ZoneGlowFloorGapFillLogic.NeedsGlowFill(receivedLight: 1, minReceivedLight: 1));
        }

        [Test]
        public void IsWithinSpacing_RespectsChebyshevDistance()
        {
            var placed = new List<Vector3Int> { new Vector3Int(10, 10, 0) };

            Assert.IsTrue(ZoneGlowFloorGapFillLogic.IsWithinSpacing(new Vector3Int(12, 10, 0), placed, 3));
            Assert.IsFalse(ZoneGlowFloorGapFillLogic.IsWithinSpacing(new Vector3Int(13, 10, 0), placed, 3));
        }

        [Test]
        public void GenerateCave_HigherWallDensity_ProducesFewerFloorCells()
        {
            var bounds = new RectInt(0, 0, 40, 40);
            var rngOpen = new System.Random(42);
            var rngWalled = new System.Random(42);

            bool[,] open = ZoneRectProcGenerator.GenerateCave(bounds, rngOpen, wallDensity: 38, ensureConnectivity: true);
            bool[,] walled = ZoneRectProcGenerator.GenerateCave(bounds, rngWalled, wallDensity: 55, ensureConnectivity: true);

            Assert.Greater(CountFloorCells(open), CountFloorCells(walled));
        }

        static int CountFloorCells(bool[,] mask)
        {
            int count = 0;
            int width = mask.GetLength(0);
            int height = mask.GetLength(1);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (mask[x, y])
                        count++;
                }
            }

            return count;
        }
    }
}
