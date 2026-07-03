using System.Collections.Generic;
using JRogue.World.Generation;
using JRogue.World.Generation.Zones;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.World
{
    [TestFixture]
    public sealed class PortalPlacementResolverTests
    {
        [Test]
        public void TryPickTaggedRegionCell_MaxManhattanFromStart_PicksFarthest()
        {
            var candidates = new List<Vector3Int>
            {
                new Vector3Int(5, 5, 0),
                new Vector3Int(12, 8, 0),
                new Vector3Int(3, 10, 0),
            };
            Vector3Int start = new Vector3Int(10, 5, 0);

            Assert.IsTrue(PortalPlacementResolver.TryPickTaggedRegionCell(
                candidates,
                start,
                TaggedRegionPortalMetric.MaxManhattanFromStart,
                rng: null,
                out Vector3Int cell));

            Assert.AreEqual(new Vector3Int(12, 8, 0), cell);
        }

        [Test]
        public void TryPickTaggedRegionCell_MaxY_PicksNorthernmost()
        {
            var candidates = new List<Vector3Int>
            {
                new Vector3Int(4, 20, 0),
                new Vector3Int(8, 27, 0),
                new Vector3Int(12, 24, 0),
            };

            Assert.IsTrue(PortalPlacementResolver.TryPickTaggedRegionCell(
                candidates,
                Vector3Int.zero,
                TaggedRegionPortalMetric.MaxY,
                rng: null,
                out Vector3Int cell));

            Assert.AreEqual(new Vector3Int(8, 27, 0), cell);
        }

        [Test]
        public void TryPickTaggedRegionCell_TieBreaksWithRng()
        {
            var candidates = new List<Vector3Int>
            {
                new Vector3Int(1, 10, 0),
                new Vector3Int(9, 10, 0),
            };

            Assert.IsTrue(PortalPlacementResolver.TryPickTaggedRegionCell(
                candidates,
                Vector3Int.zero,
                TaggedRegionPortalMetric.MaxY,
                new System.Random(0),
                out Vector3Int cell));

            Assert.AreEqual(10, cell.y);
        }

        [Test]
        public void ResolveStampPortalCell_PrefersMarkerOverPortalCell()
        {
            var stamp = ScriptableObject.CreateInstance<DungeonLayoutStamp>();
            try
            {
                stamp.InitializeGrid(10, 10, borderWalls: true);
                stamp.SetMarker("portal_north", new Vector3Int(5, 8, 0));

                var rule = new PortalPlacementRule
                {
                    portalMarkerId = "portal_north",
                    portalCell = new Vector3Int(1, 1, 0),
                };

                Assert.AreEqual(
                    new Vector3Int(5, 8, 0),
                    PortalPlacementResolver.ResolveStampPortalCell(stamp, rule));
            }
            finally
            {
                Object.DestroyImmediate(stamp);
            }
        }

        [Test]
        public void ResolveStampPortalCell_FallsBackToPortalCell()
        {
            var stamp = ScriptableObject.CreateInstance<DungeonLayoutStamp>();
            try
            {
                stamp.InitializeGrid(10, 10, borderWalls: true);
                var rule = new PortalPlacementRule
                {
                    portalCell = new Vector3Int(2, 3, 0),
                };

                Assert.AreEqual(
                    new Vector3Int(2, 3, 0),
                    PortalPlacementResolver.ResolveStampPortalCell(stamp, rule));
            }
            finally
            {
                Object.DestroyImmediate(stamp);
            }
        }

        [Test]
        public void ScoreCell_MinY_PrefersSouthernCells()
        {
            int south = PortalPlacementResolver.ScoreCell(
                new Vector3Int(4, 2, 0),
                Vector3Int.zero,
                TaggedRegionPortalMetric.MinY);
            int north = PortalPlacementResolver.ScoreCell(
                new Vector3Int(4, 20, 0),
                Vector3Int.zero,
                TaggedRegionPortalMetric.MinY);

            Assert.Greater(south, north);
        }

        [Test]
        public void FixedMapRowPick_SameSeedPicksSameCandidate()
        {
            var candidates = new List<Vector3Int>();
            for (int x = 0; x < 50; x++)
                candidates.Add(new Vector3Int(x, 79, 0));

            var rngA = ZoneGenerationRng.CreatePopulationRng(4242, "dungeon_floor_01portal_floor02");
            var rngB = ZoneGenerationRng.CreatePopulationRng(4242, "dungeon_floor_01portal_floor02");
            Vector3Int pickA = candidates[rngA.Next(candidates.Count)];
            Vector3Int pickB = candidates[rngB.Next(candidates.Count)];

            Assert.AreEqual(pickA, pickB);
            Assert.AreEqual(79, pickA.y);
        }

        [Test]
        public void FixedMapRowPick_DifferentSeedsMayDiffer()
        {
            var candidates = new List<Vector3Int>();
            for (int x = 0; x < 50; x++)
                candidates.Add(new Vector3Int(x, 79, 0));

            var rngA = ZoneGenerationRng.CreatePopulationRng(1, "dungeon_floor_01portal_floor02");
            var rngB = ZoneGenerationRng.CreatePopulationRng(999_999, "dungeon_floor_01portal_floor02");
            Vector3Int pickA = candidates[rngA.Next(candidates.Count)];
            Vector3Int pickB = candidates[rngB.Next(candidates.Count)];

            Assert.AreNotEqual(pickA.x, pickB.x);
        }
    }
}
