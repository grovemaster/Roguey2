using NUnit.Framework;
using UnityEngine;
using JRogue.Pathfinding;

namespace JRogue.Tests.UnitTests.Pathfinding
{
    [TestFixture]
    public class GridAStarPathfinderTest
    {
        [Test]
        public void OctileHeuristic_ExampleFromSpec_MatchesExpected()
        {
            Vector3Int a = new Vector3Int(0, 0, 0);
            Vector3Int b = new Vector3Int(3, 4, 0);
            int h = GridAStarPathfinder.OctileHeuristic(a, b);
            // dx=3, dy=4: 10*(3+4) + (14-20)*min(3,4) = 70 - 18 = 52
            Assert.AreEqual(52, h);
        }

        [Test]
        public void TryGetFirstStepInternal_OpenGrid_PrefersDiagonalWhenCheaper()
        {
            bool Open(Vector3Int _) => true;
            bool Corners(Vector3Int _, Vector3Int __) => true;

            Vector3Int start = new Vector3Int(0, 0, 0);
            Vector3Int goal = new Vector3Int(2, 2, 0);

            bool ok = GridAStarPathfinder.TryGetFirstStepInternal(start, goal, Open, Corners, out Vector3Int step);
            Assert.IsTrue(ok);
            Assert.AreEqual(new Vector3Int(1, 1, 0), step);
        }

        [Test]
        public void TryGetFirstStepInternal_CornerCutBlocked_SkipsDiagonalSqueeze()
        {
            bool Open(Vector3Int _) => true;

            bool Corners(Vector3Int from, Vector3Int to)
            {
                Vector3Int d = to - from;
                if (d.x != 0 && d.y != 0 && from == new Vector3Int(0, 0, 0) && to == new Vector3Int(1, 1, 0))
                    return false;
                return true;
            }

            Vector3Int start = new Vector3Int(0, 0, 0);
            Vector3Int goal = new Vector3Int(2, 2, 0);

            bool ok = GridAStarPathfinder.TryGetFirstStepInternal(start, goal, Open, Corners, out Vector3Int step);
            Assert.IsTrue(ok);
            Assert.AreNotEqual(new Vector3Int(1, 1, 0), step);
            Assert.AreEqual(1, Mathf.Abs(step.x - start.x) + Mathf.Abs(step.y - start.y));
        }
    }
}
