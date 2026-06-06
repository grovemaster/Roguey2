using System.Collections.Generic;
using JRogue.World.Generation.Zones;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.World
{
    [TestFixture]
    public sealed class ZoneCompassRectResolverTests
    {
        [Test]
        public void ResolveCompassPreset_Floor01Partitions_DoNotOverlap()
        {
            RectInt center = ZoneCompassRectResolver.ResolveCompassPreset(
                CompassDirection.Center,
                30,
                30);
            RectInt north = ZoneCompassRectResolver.ResolveCompassPreset(
                CompassDirection.North,
                30,
                30);
            RectInt east = ZoneCompassRectResolver.ResolveCompassPreset(
                CompassDirection.East,
                30,
                30);

            Assert.IsFalse(ZoneCompassRectResolver.RectsOverlap(center, north));
            Assert.IsFalse(ZoneCompassRectResolver.RectsOverlap(center, east));
            Assert.IsFalse(ZoneCompassRectResolver.RectsOverlap(north, east));
        }

        [Test]
        public void ResolveCompassPreset_NorthBand_StartsAtPartitionRow()
        {
            RectInt north = ZoneCompassRectResolver.ResolveCompassPreset(
                CompassDirection.North,
                30,
                30);

            Assert.AreEqual(0, north.xMin);
            Assert.AreEqual(20, north.yMin);
            Assert.AreEqual(30, north.xMax);
            Assert.AreEqual(30, north.yMax);
        }

        [Test]
        public void ResolveCompassPreset_CenterAndEast_ShareLowerBandWithoutOverlap()
        {
            RectInt center = ZoneCompassRectResolver.ResolveCompassPreset(
                CompassDirection.Center,
                30,
                30);
            RectInt east = ZoneCompassRectResolver.ResolveCompassPreset(
                CompassDirection.East,
                30,
                30);

            Assert.AreEqual(19, center.yMax);
            Assert.AreEqual(20, east.xMin);
            Assert.IsFalse(ZoneCompassRectResolver.RectsOverlap(center, east));
        }
    }

    [TestFixture]
    public sealed class ZoneSelectionSolverTests
    {
        readonly List<Object> _assets = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _assets.Count; i++)
            {
                if (_assets[i] != null)
                    Object.DestroyImmediate(_assets[i]);
            }

            _assets.Clear();
        }

        [Test]
        public void Resolve_MandatoryCenter_AlwaysIncludesDungeon()
        {
            DungeonFloorZoneLayout layout = CreateFloor01Layout();
            var rng = new System.Random(12345);

            ZoneSelectionResult result = ZoneSelectionSolver.Resolve(layout, rng);

            Assert.IsTrue(result.Success);
            Assert.IsTrue(ContainsZone(result.Pieces, "dungeon"));
        }

        [Test]
        public void Resolve_SameSeed_ProducesSameOptionalZones()
        {
            DungeonFloorZoneLayout layoutA = CreateFloor01Layout();
            DungeonFloorZoneLayout layoutB = CreateFloor01Layout();
            var rngA = new System.Random(999);
            var rngB = new System.Random(999);

            ZoneSelectionResult a = ZoneSelectionSolver.Resolve(layoutA, rngA);
            ZoneSelectionResult b = ZoneSelectionSolver.Resolve(layoutB, rngB);

            Assert.IsTrue(a.Success);
            Assert.IsTrue(b.Success);
            Assert.AreEqual(OptionalBiomeSignature(a.Pieces), OptionalBiomeSignature(b.Pieces));
        }

        [Test]
        public void Resolve_DifferentSeeds_CanDiffer()
        {
            ZoneSelectionResult a = ZoneSelectionSolver.Resolve(CreateFloor01Layout(), new System.Random(1));
            ZoneSelectionResult b = ZoneSelectionSolver.Resolve(CreateFloor01Layout(), new System.Random(2));

            Assert.IsTrue(a.Success);
            Assert.IsTrue(b.Success);
            Assert.AreNotEqual(OptionalBiomeSignature(a.Pieces), OptionalBiomeSignature(b.Pieces));
        }

        [Test]
        public void Resolve_Floor01Layout_NeverUsesMandatoryFallback()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                ZoneSelectionResult result = ZoneSelectionSolver.Resolve(
                    CreateFloor01Layout(),
                    new System.Random(seed));
                Assert.IsTrue(result.Success, $"seed {seed}");
                Assert.IsNull(result.FailureReason, $"seed {seed} fell back: {result.FailureReason}");
            }
        }

        [Test]
        public void Resolve_NeverSelectsDesertAndSnowTogether()
        {
            for (int seed = 0; seed < 100; seed++)
            {
                ZoneSelectionResult result = ZoneSelectionSolver.Resolve(
                    CreateFloor01Layout(),
                    new System.Random(seed));
                Assert.IsTrue(result.Success);
                Assert.IsFalse(
                    ContainsZone(result.Pieces, "desert") && ContainsZone(result.Pieces, "snow"),
                    $"seed {seed} selected both desert and snow");
            }
        }

        static string OptionalBiomeSignature(ResolvedZonePiece[] pieces)
        {
            bool desert = ContainsZone(pieces, "desert");
            bool snow = ContainsZone(pieces, "snow");
            return $"d={desert};s={snow}";
        }

        static bool ContainsZone(ResolvedZonePiece[] pieces, string zoneId)
        {
            for (int i = 0; i < pieces.Length; i++)
            {
                if (pieces[i].ZoneId == zoneId)
                    return true;
            }

            return false;
        }

        DungeonFloorZoneLayout CreateFloor01Layout()
        {
            var layout = ScriptableObject.CreateInstance<DungeonFloorZoneLayout>();
            layout.ReplaceAuthoringData(
                30,
                30,
                ZoneLayoutKind.CompassSlots,
                ZoneIds.Rock,
                new[]
                {
                    new ZoneSelectionRule
                    {
                        zoneId = "desert",
                        excludes = new[] { "snow" },
                        maxInstances = 1,
                    },
                    new ZoneSelectionRule
                    {
                        zoneId = "snow",
                        excludes = new[] { "desert" },
                        maxInstances = 1,
                    },
                },
                new[]
                {
                    new ZoneLayoutPiece
                    {
                        pieceId = "center",
                        anchorKind = ZonePieceAnchorKind.Compass,
                        compassDirection = CompassDirection.Center,
                        mandatory = true,
                        isPlayerStartPiece = true,
                        candidates = new[]
                        {
                            new ZoneLayoutPieceCandidate { zoneId = "dungeon", weight = 1 },
                        },
                    },
                    new ZoneLayoutPiece
                    {
                        pieceId = "north",
                        anchorKind = ZonePieceAnchorKind.Compass,
                        compassDirection = CompassDirection.North,
                        candidates = new[]
                        {
                            new ZoneLayoutPieceCandidate { zoneId = "snow", weight = 40 },
                            new ZoneLayoutPieceCandidate { zoneId = ZoneIds.Empty, weight = 60 },
                        },
                    },
                    new ZoneLayoutPiece
                    {
                        pieceId = "east",
                        anchorKind = ZonePieceAnchorKind.Compass,
                        compassDirection = CompassDirection.East,
                        candidates = new[]
                        {
                            new ZoneLayoutPieceCandidate { zoneId = "desert", weight = 55 },
                            new ZoneLayoutPieceCandidate { zoneId = ZoneIds.Empty, weight = 45 },
                        },
                    },
                });
            _assets.Add(layout);
            return layout;
        }
    }

    [TestFixture]
    public sealed class ZoneCellMapBuilderTests
    {
        [Test]
        public void Build_AssignsZoneIdsToPieceRects()
        {
            var pieces = new[]
            {
                new ResolvedZonePiece("center", "dungeon", ZoneCompassRectResolver.FromInclusiveBounds(0, 0, 10, 10), true),
                new ResolvedZonePiece("north", "snow", ZoneCompassRectResolver.FromInclusiveBounds(0, 11, 10, 15), false),
            };

            Dictionary<Vector3Int, string> map = ZoneCellMapBuilder.Build(20, 20, ZoneIds.Rock, pieces);

            Assert.AreEqual("dungeon", map[new Vector3Int(5, 5, 0)]);
            Assert.AreEqual("snow", map[new Vector3Int(5, 12, 0)]);
            Assert.AreEqual(ZoneIds.Rock, map[new Vector3Int(19, 19, 0)]);
        }
    }
}
