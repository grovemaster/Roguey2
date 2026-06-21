using System.Collections.Generic;
using JRogue.World.Generation.Zones;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.World
{
    [TestFixture]
    public sealed class ZoneOpeningConnectivityTests
    {
        [Test]
        public void ConnectOpeningCells_IsolatedOpening_CarvesTowardInterior()
        {
            var bounds = new RectInt(0, 0, 10, 10);
            var floor = new bool[10, 10];
            for (int y = 1; y < 9; y++)
            {
                for (int x = 1; x < 9; x++)
                    floor[x, y] = x >= 4 && x <= 6 && y >= 4 && y <= 6;
            }

            var openings = new List<Vector2Int> { new(5, 9) };
            ZoneRectProcGenerator.ConnectOpeningCells(floor, bounds, openings);

            Assert.IsTrue(floor[5, 9]);
            Assert.IsTrue(ZoneRectProcGenerator.IsOpeningConnectedToInterior(floor, new Vector2Int(5, 9)));
        }

        [Test]
        public void IsOpeningConnectedToInterior_SingleTile_ReturnsFalse()
        {
            var floor = new bool[5, 5];
            floor[2, 2] = true;
            Assert.IsFalse(ZoneRectProcGenerator.IsOpeningConnectedToInterior(floor, new Vector2Int(2, 2)));
        }
    }
}
