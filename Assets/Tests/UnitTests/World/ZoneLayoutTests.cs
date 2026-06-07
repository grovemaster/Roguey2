using System.Collections.Generic;
using JRogue.World.Generation;
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

    [TestFixture]
    public sealed class ZoneInterfaceResolverTests
    {
        [Test]
        public void ResolveInterfaces_Floor01Pieces_IncludeCenterNorthAdjacency()
        {
            var pieces = new[]
            {
                new ResolvedZonePiece(
                    "center",
                    "dungeon",
                    ZoneCompassRectResolver.ResolveCompassPreset(CompassDirection.Center, 30, 30),
                    true),
                new ResolvedZonePiece(
                    "north",
                    "snow",
                    ZoneCompassRectResolver.ResolveCompassPreset(CompassDirection.North, 30, 30),
                    false),
            };

            List<ZoneInterface> interfaces = ZoneInterfaceResolver.ResolveInterfaces(pieces, 30, 30);
            bool found = false;
            for (int i = 0; i < interfaces.Count; i++)
            {
                ZoneInterface iface = interfaces[i];
                if (iface.PieceAId == "center"
                    && iface.PieceBId == "north"
                    && iface.EdgeOnA == ZoneInterfaceEdge.North)
                {
                    found = true;
                    Assert.Greater(iface.SpanMax, iface.SpanMin);
                    break;
                }
            }

            Assert.IsTrue(found, "Expected center→north interface on shared band edge.");
        }
    }

    [TestFixture]
    public sealed class ZoneBoundaryResolverTests
    {
        [Test]
        public void ResolveKind_HabitatNeighbors_UsesDefaultOpen()
        {
            var layout = ScriptableObject.CreateInstance<DungeonFloorZoneLayout>();
            layout.ReplaceAuthoringData(
                30,
                30,
                ZoneLayoutKind.CompassSlots,
                ZoneIds.Rock,
                new ZoneSelectionRule[0],
                new[]
                {
                    new ZoneLayoutPiece
                    {
                        pieceId = "center",
                        defaultBoundary = ZoneBoundaryKind.Open,
                    },
                });

            var center = new ResolvedZonePiece(
                "center",
                "dungeon",
                ZoneCompassRectResolver.ResolveCompassPreset(CompassDirection.Center, 30, 30),
                true);
            var north = new ResolvedZonePiece(
                "north",
                "snow",
                ZoneCompassRectResolver.ResolveCompassPreset(CompassDirection.North, 30, 30),
                false);

            layout.TryGetLayoutPiece("center", out ZoneLayoutPiece layoutPiece);
            var iface = new ZoneInterface("center", "north", ZoneInterfaceEdge.North, 0, 21, 19);

            ZoneBoundaryKind kind = ZoneBoundaryResolver.ResolveKind(layout, layoutPiece, iface, center, north);

            Assert.AreEqual(ZoneBoundaryKind.Open, kind);
            Object.DestroyImmediate(layout);
        }

        [Test]
        public void ResolveKind_EmptyNeighbor_UsesWall()
        {
            var layout = ScriptableObject.CreateInstance<DungeonFloorZoneLayout>();
            layout.ReplaceAuthoringData(
                30,
                30,
                ZoneLayoutKind.CompassSlots,
                ZoneIds.Rock,
                new ZoneSelectionRule[0],
                new[]
                {
                    new ZoneLayoutPiece
                    {
                        pieceId = "center",
                        defaultBoundary = ZoneBoundaryKind.Open,
                    },
                });

            var center = new ResolvedZonePiece(
                "center",
                "dungeon",
                ZoneCompassRectResolver.ResolveCompassPreset(CompassDirection.Center, 30, 30),
                true);
            var north = new ResolvedZonePiece(
                "north",
                ZoneIds.Empty,
                ZoneCompassRectResolver.ResolveCompassPreset(CompassDirection.North, 30, 30),
                false);

            layout.TryGetLayoutPiece("center", out ZoneLayoutPiece layoutPiece);
            var iface = new ZoneInterface("center", "north", ZoneInterfaceEdge.North, 0, 21, 19);

            ZoneBoundaryKind kind = ZoneBoundaryResolver.ResolveKind(layout, layoutPiece, iface, center, north);

            Assert.AreEqual(ZoneBoundaryKind.Wall, kind);
            Object.DestroyImmediate(layout);
        }
    }

    [TestFixture]
    public sealed class ZoneSubStampPlayerStartTests
    {
        [Test]
        public void TryResolveSubStampPlayerStart_MapsStampMarkerIntoPieceBounds()
        {
            var stamp = ScriptableObject.CreateInstance<DungeonLayoutStamp>();
            stamp.InitializeGrid(20, 20);
            stamp.SetMarker(StampMarkerIds.PlayerStart, new Vector3Int(10, 5, 0));

            var profile = new ZoneFillProfile
            {
                mode = ZoneFillMode.SubStamp,
                subStampTable = new[]
                {
                    new ZoneSubStampEntry { stamp = stamp, weight = 1 },
                },
            };

            var piece = new ResolvedZonePiece(
                "center",
                "dungeon",
                ZoneCompassRectResolver.ResolveCompassPreset(CompassDirection.Center, 30, 30),
                true);

            bool resolved = ZonePieceFiller.TryResolveSubStampPlayerStart(
                piece,
                profile,
                new System.Random(123),
                out Vector3Int worldCell);

            Assert.IsTrue(resolved);
            Assert.AreEqual(new Vector3Int(10, 5, 0), worldCell);
            Object.DestroyImmediate(stamp);
        }

        [Test]
        public void SubStampBorderStrip_AllowsReachabilityToStampNorthEdge()
        {
            var stamp = ScriptableObject.CreateInstance<DungeonLayoutStamp>();
            stamp.InitializeGrid(20, 20);

            int reachableBefore = CountReachable(stamp, 10, 5);
            int northEdgeBefore = CountReachableOnRow(stamp, 10, 5, 19);

            Assert.Greater(reachableBefore, 0);
            Assert.AreEqual(0, northEdgeBefore, "Sealed substamp border should block north edge before strip.");

            int reachableAfter = CountReachableWithStrippedBorder(stamp, 10, 5);
            int northEdgeAfter = CountReachableOnRowWithStrippedBorder(stamp, 10, 5, 19);

            Assert.Greater(reachableAfter, reachableBefore - 50);
            Assert.Greater(northEdgeAfter, 0, "Stripped border should expose north edge floor.");
            Object.DestroyImmediate(stamp);
        }

        static int CountReachable(DungeonLayoutStamp stamp, int sx, int sy)
        {
            var seen = new HashSet<Vector2Int> { new Vector2Int(sx, sy) };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(new Vector2Int(sx, sy));

            while (queue.Count > 0)
            {
                Vector2Int c = queue.Dequeue();
                TryWalk(stamp, c.x + 1, c.y, seen, queue);
                TryWalk(stamp, c.x - 1, c.y, seen, queue);
                TryWalk(stamp, c.x, c.y + 1, seen, queue);
                TryWalk(stamp, c.x, c.y - 1, seen, queue);
            }

            return seen.Count;
        }

        static int CountReachableOnRow(DungeonLayoutStamp stamp, int sx, int sy, int targetY) =>
            CountReachable(stamp, sx, sy) > 0
                ? CountReachableRow(stamp, sx, sy, targetY, stripBorder: false)
                : 0;

        static int CountReachableOnRowWithStrippedBorder(DungeonLayoutStamp stamp, int sx, int sy, int targetY) =>
            CountReachableRow(stamp, sx, sy, targetY, stripBorder: true);

        static int CountReachableWithStrippedBorder(DungeonLayoutStamp stamp, int sx, int sy)
        {
            var seen = new HashSet<Vector2Int> { new Vector2Int(sx, sy) };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(new Vector2Int(sx, sy));

            while (queue.Count > 0)
            {
                Vector2Int c = queue.Dequeue();
                TryWalkStripped(stamp, c.x + 1, c.y, seen, queue);
                TryWalkStripped(stamp, c.x - 1, c.y, seen, queue);
                TryWalkStripped(stamp, c.x, c.y + 1, seen, queue);
                TryWalkStripped(stamp, c.x, c.y - 1, seen, queue);
            }

            return seen.Count;
        }

        static int CountReachableRow(DungeonLayoutStamp stamp, int sx, int sy, int targetY, bool stripBorder)
        {
            var seen = new HashSet<Vector2Int> { new Vector2Int(sx, sy) };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(new Vector2Int(sx, sy));

            while (queue.Count > 0)
            {
                Vector2Int c = queue.Dequeue();
                if (stripBorder)
                {
                    TryWalkStripped(stamp, c.x + 1, c.y, seen, queue);
                    TryWalkStripped(stamp, c.x - 1, c.y, seen, queue);
                    TryWalkStripped(stamp, c.x, c.y + 1, seen, queue);
                    TryWalkStripped(stamp, c.x, c.y - 1, seen, queue);
                }
                else
                {
                    TryWalk(stamp, c.x + 1, c.y, seen, queue);
                    TryWalk(stamp, c.x - 1, c.y, seen, queue);
                    TryWalk(stamp, c.x, c.y + 1, seen, queue);
                    TryWalk(stamp, c.x, c.y - 1, seen, queue);
                }
            }

            int count = 0;
            for (int x = 0; x < stamp.Width; x++)
            {
                if (seen.Contains(new Vector2Int(x, targetY)))
                    count++;
            }

            return count;
        }

        static void TryWalk(
            DungeonLayoutStamp stamp,
            int x,
            int y,
            HashSet<Vector2Int> seen,
            Queue<Vector2Int> queue)
        {
            if (x < 0 || y < 0 || x >= stamp.Width || y >= stamp.Height)
                return;

            if (!stamp.IsFloor(x, y) || stamp.IsWall(x, y))
                return;

            var cell = new Vector2Int(x, y);
            if (!seen.Add(cell))
                return;

            queue.Enqueue(cell);
        }

        static void TryWalkStripped(
            DungeonLayoutStamp stamp,
            int x,
            int y,
            HashSet<Vector2Int> seen,
            Queue<Vector2Int> queue)
        {
            if (x < 0 || y < 0 || x >= stamp.Width || y >= stamp.Height)
                return;

            if (!IsWalkableWithStrippedBorder(stamp, x, y))
                return;

            var cell = new Vector2Int(x, y);
            if (!seen.Add(cell))
                return;

            queue.Enqueue(cell);
        }

        static bool IsWalkableWithStrippedBorder(DungeonLayoutStamp stamp, int x, int y)
        {
            if (x <= 0 || y <= 0 || x >= stamp.Width - 1 || y >= stamp.Height - 1)
                return true;

            return stamp.IsFloor(x, y) && !stamp.IsWall(x, y);
        }
    }

    [TestFixture]
    public sealed class ZoneBoundaryApplicatorTests
    {
        [Test]
        public void ApplyAll_ExteriorBoundary_DoesNotThrowWhenPieceBMissing()
        {
            var layout = ScriptableObject.CreateInstance<DungeonFloorZoneLayout>();
            layout.ReplaceAuthoringData(
                30,
                30,
                ZoneLayoutKind.CompassSlots,
                ZoneIds.Rock,
                new ZoneSelectionRule[0],
                new ZoneLayoutPiece[0]);

            var center = new ResolvedZonePiece(
                "center",
                "dungeon",
                ZoneCompassRectResolver.ResolveCompassPreset(CompassDirection.Center, 30, 30),
                true);

            var boundaries = new List<ResolvedZoneBoundary>
            {
                new ResolvedZoneBoundary(
                    new ZoneInterface(
                        "center",
                        ZoneIds.ExteriorNeighbor,
                        ZoneInterfaceEdge.West,
                        0,
                        20,
                        0),
                    ZoneBoundaryKind.Wall,
                    1,
                    1),
            };

            Assert.DoesNotThrow(() =>
            {
                ZoneBoundaryStats stats = ZoneBoundaryApplicator.ApplyAll(
                    null,
                    null,
                    layout,
                    new[] { center },
                    boundaries,
                    default);
                Assert.AreEqual(0, stats.OpenCells);
            });

            Object.DestroyImmediate(layout);
        }
    }

    [TestFixture]
    public sealed class ZoneJigsawSolverTests
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
        public void TryPackPieces_ThreeConnectedMandatoryPieces_DoNotOverlap()
        {
            DungeonFloorZoneLayout layout = CreateThreePieceJigsawLayout();
            var assignments = new List<ZoneJigsawAssignment>
            {
                new ZoneJigsawAssignment(layout.Pieces[0], "orc_castle"),
                new ZoneJigsawAssignment(layout.Pieces[1], "witch_forest"),
                new ZoneJigsawAssignment(layout.Pieces[2], "mountain"),
            };

            Assert.IsTrue(
                ZoneJigsawSolver.TryPackPieces(layout, assignments, new System.Random(42), out ResolvedZonePiece[] resolved));
            Assert.AreEqual(3, resolved.Length);

            for (int i = 0; i < resolved.Length; i++)
            {
                for (int j = i + 1; j < resolved.Length; j++)
                {
                    Assert.IsFalse(
                        ZoneCompassRectResolver.RectsOverlap(resolved[i].Bounds, resolved[j].Bounds),
                        $"{resolved[i].PieceId} overlaps {resolved[j].PieceId}");
                }
            }

            Assert.IsTrue(
                ZoneJigsawSolver.SharesEdge(FindPiece(resolved, "west").Bounds, FindPiece(resolved, "center").Bounds));
            Assert.IsTrue(
                ZoneJigsawSolver.SharesEdge(FindPiece(resolved, "center").Bounds, FindPiece(resolved, "east").Bounds));
        }

        static ResolvedZonePiece FindPiece(ResolvedZonePiece[] pieces, string pieceId)
        {
            for (int i = 0; i < pieces.Length; i++)
            {
                if (pieces[i].PieceId == pieceId)
                    return pieces[i];
            }

            Assert.Fail($"Missing piece {pieceId}");
            return default;
        }

        DungeonFloorZoneLayout CreateThreePieceJigsawLayout()
        {
            var layout = ScriptableObject.CreateInstance<DungeonFloorZoneLayout>();
            _assets.Add(layout);

            layout.ReplaceAuthoringData(
                40,
                30,
                ZoneLayoutKind.ExplicitPieces,
                ZoneIds.Rock,
                new ZoneSelectionRule[0],
                new[]
                {
                    new ZoneLayoutPiece
                    {
                        pieceId = "west",
                        mandatory = true,
                        isPlayerStartPiece = true,
                        connectsTo = new[] { "center" },
                        candidates = new[]
                        {
                            new ZoneLayoutPieceCandidate { zoneId = "orc_castle", weight = 1 },
                        },
                    },
                    new ZoneLayoutPiece
                    {
                        pieceId = "center",
                        mandatory = true,
                        connectsTo = new[] { "west", "east" },
                        candidates = new[]
                        {
                            new ZoneLayoutPieceCandidate { zoneId = "witch_forest", weight = 1 },
                        },
                    },
                    new ZoneLayoutPiece
                    {
                        pieceId = "east",
                        mandatory = true,
                        connectsTo = new[] { "center" },
                        candidates = new[]
                        {
                            new ZoneLayoutPieceCandidate { zoneId = "mountain", weight = 1 },
                        },
                    },
                },
                new[]
                {
                    CreateZoneDefinition("orc_castle", 10, 10, 14, 14),
                    CreateZoneDefinition("witch_forest", 12, 10, 16, 14),
                    CreateZoneDefinition("mountain", 10, 10, 14, 14),
                });

            return layout;
        }

        DungeonZoneDefinition CreateZoneDefinition(
            string zoneId,
            int minWidth,
            int minHeight,
            int maxWidth,
            int maxHeight)
        {
            var definition = ScriptableObject.CreateInstance<DungeonZoneDefinition>();
            _assets.Add(definition);
            SetField(definition, "zoneId", zoneId);
            SetField(definition, "minWidth", minWidth);
            SetField(definition, "minHeight", minHeight);
            SetField(definition, "maxWidth", maxWidth);
            SetField(definition, "maxHeight", maxHeight);
            return definition;
        }

        static void SetField(Object target, string fieldName, object value)
        {
            target.GetType()
                .GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(target, value);
        }
    }

    [TestFixture]
    public sealed class ZoneRectProcGeneratorTests
    {
        [Test]
        public void GenerateRoomCorridor_ProducesConnectedWalkableCells()
        {
            bool[,] mask = ZoneRectProcGenerator.GenerateRoomCorridor(
                new RectInt(0, 0, 20, 15),
                new System.Random(1),
                ensureConnectivity: true);

            Assert.NotNull(mask);
            Assert.Greater(CountFloorCells(mask), 24);
            Assert.AreEqual(1, CountFloorComponents(mask));
        }

        [Test]
        public void GenerateCave_SameSeed_ProducesIdenticalMask()
        {
            var bounds = new RectInt(0, 0, 18, 14);
            bool[,] a = ZoneRectProcGenerator.GenerateCave(bounds, new System.Random(77), 35, true);
            bool[,] b = ZoneRectProcGenerator.GenerateCave(bounds, new System.Random(77), 35, true);

            Assert.NotNull(a);
            Assert.NotNull(b);
            Assert.AreEqual(CountFloorCells(a), CountFloorCells(b));
            Assert.AreEqual(MaskSignature(a), MaskSignature(b));
        }

        static int CountFloorCells(bool[,] mask)
        {
            int count = 0;
            for (int y = 0; y < mask.GetLength(1); y++)
            {
                for (int x = 0; x < mask.GetLength(0); x++)
                {
                    if (mask[x, y])
                        count++;
                }
            }

            return count;
        }

        static int CountFloorComponents(bool[,] mask)
        {
            int width = mask.GetLength(0);
            int height = mask.GetLength(1);
            var visited = new bool[width, height];
            int components = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (!mask[x, y] || visited[x, y])
                        continue;

                    components++;
                    Flood(mask, visited, x, y);
                }
            }

            return components;
        }

        static void Flood(bool[,] mask, bool[,] visited, int startX, int startY)
        {
            int width = mask.GetLength(0);
            int height = mask.GetLength(1);
            var stack = new Stack<Vector2Int>();
            stack.Push(new Vector2Int(startX, startY));

            while (stack.Count > 0)
            {
                Vector2Int cell = stack.Pop();
                if (cell.x < 0 || cell.y < 0 || cell.x >= width || cell.y >= height)
                    continue;

                if (visited[cell.x, cell.y] || !mask[cell.x, cell.y])
                    continue;

                visited[cell.x, cell.y] = true;
                stack.Push(cell + Vector2Int.up);
                stack.Push(cell + Vector2Int.down);
                stack.Push(cell + Vector2Int.left);
                stack.Push(cell + Vector2Int.right);
            }
        }

        static string MaskSignature(bool[,] mask)
        {
            var builder = new System.Text.StringBuilder();
            for (int y = 0; y < mask.GetLength(1); y++)
            {
                for (int x = 0; x < mask.GetLength(0); x++)
                    builder.Append(mask[x, y] ? '1' : '0');
            }

            return builder.ToString();
        }
    }

    [TestFixture]
    public sealed class ZoneSelectionExplicitPiecesTests
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
        public void Resolve_ExplicitPiecesLayout_PacksAllMandatoryZones()
        {
            DungeonFloorZoneLayout layout = CreateExplicitPiecesLayout();
            ZoneSelectionResult result = ZoneSelectionSolver.Resolve(layout, new System.Random(12345));

            Assert.IsTrue(result.Success, result.FailureReason);
            Assert.AreEqual(3, result.Pieces.Length);
            Assert.IsTrue(ContainsZone(result.Pieces, "orc_castle"));
            Assert.IsTrue(ContainsZone(result.Pieces, "witch_forest"));
            Assert.IsTrue(ContainsZone(result.Pieces, "mountain"));
        }

        [Test]
        public void Resolve_ExplicitPiecesLayout_PlayerStartOnOrcCastleWest()
        {
            DungeonFloorZoneLayout layout = CreateExplicitPiecesLayout();
            ZoneSelectionResult result = ZoneSelectionSolver.Resolve(layout, new System.Random(12345));

            Assert.IsTrue(result.Success, result.FailureReason);
            ResolvedZonePiece start = default;
            bool foundStart = false;
            for (int i = 0; i < result.Pieces.Length; i++)
            {
                if (!result.Pieces[i].IsPlayerStartPiece)
                    continue;

                start = result.Pieces[i];
                foundStart = true;
                break;
            }

            Assert.IsTrue(foundStart);
            Assert.AreEqual("west", start.PieceId);
            Assert.AreEqual("orc_castle", start.ZoneId);
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

        DungeonFloorZoneLayout CreateExplicitPiecesLayout()
        {
            var layout = ScriptableObject.CreateInstance<DungeonFloorZoneLayout>();
            _assets.Add(layout);

            var orc = ScriptableObject.CreateInstance<DungeonZoneDefinition>();
            var witch = ScriptableObject.CreateInstance<DungeonZoneDefinition>();
            var mountain = ScriptableObject.CreateInstance<DungeonZoneDefinition>();
            _assets.Add(orc);
            _assets.Add(witch);
            _assets.Add(mountain);

            SetField(orc, "zoneId", "orc_castle");
            SetField(witch, "zoneId", "witch_forest");
            SetField(mountain, "zoneId", "mountain");
            SetField(orc, "minWidth", 10);
            SetField(orc, "minHeight", 10);
            SetField(orc, "maxWidth", 12);
            SetField(orc, "maxHeight", 12);
            SetField(witch, "minWidth", 12);
            SetField(witch, "minHeight", 10);
            SetField(witch, "maxWidth", 14);
            SetField(witch, "maxHeight", 12);
            SetField(mountain, "minWidth", 10);
            SetField(mountain, "minHeight", 10);
            SetField(mountain, "maxWidth", 12);
            SetField(mountain, "maxHeight", 12);

            layout.ReplaceAuthoringData(
                40,
                30,
                ZoneLayoutKind.ExplicitPieces,
                ZoneIds.Rock,
                new[]
                {
                    new ZoneSelectionRule
                    {
                        zoneId = "witch_forest",
                        requiresAll = new[] { "orc_castle" },
                    },
                },
                new[]
                {
                    new ZoneLayoutPiece
                    {
                        pieceId = "west",
                        mandatory = true,
                        isPlayerStartPiece = true,
                        connectsTo = new[] { "center" },
                        candidates = new[]
                        {
                            new ZoneLayoutPieceCandidate { zoneId = "orc_castle", weight = 1 },
                        },
                    },
                    new ZoneLayoutPiece
                    {
                        pieceId = "center",
                        mandatory = true,
                        connectsTo = new[] { "west", "east" },
                        candidates = new[]
                        {
                            new ZoneLayoutPieceCandidate { zoneId = "witch_forest", weight = 1 },
                        },
                    },
                    new ZoneLayoutPiece
                    {
                        pieceId = "east",
                        mandatory = true,
                        connectsTo = new[] { "center" },
                        candidates = new[]
                        {
                            new ZoneLayoutPieceCandidate { zoneId = "mountain", weight = 1 },
                        },
                    },
                },
                new[] { orc, witch, mountain });

            return layout;
        }

        static void SetField(Object target, string fieldName, object value)
        {
            target.GetType()
                .GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(target, value);
        }
    }

    [TestFixture]
    public sealed class ZoneHybridCellAssignerTests
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
        public void Assign_SplitsSkeletonWalkableBetweenSeedPieces()
        {
            var stamp = ScriptableObject.CreateInstance<DungeonLayoutStamp>();
            _assets.Add(stamp);
            stamp.InitializeGrid(20, 10, borderWalls: true);
            for (int x = 1; x < 19; x++)
                stamp.SetCell(x, 4, floor: true, wall: false);

            var west = new ResolvedZonePiece("west", "dungeon", new RectInt(1, 1, 8, 8), false);
            var east = new ResolvedZonePiece("east", "desert", new RectInt(11, 1, 8, 8), false);
            var seeds = new[] { west, east };

            Dictionary<Vector3Int, string> map = ZoneHybridCellAssigner.Assign(
                stamp,
                20,
                10,
                ZoneIds.Rock,
                seeds,
                out ResolvedZonePiece[] rebuilt);

            int dungeonCells = 0;
            int desertCells = 0;
            foreach (KeyValuePair<Vector3Int, string> entry in map)
            {
                if (entry.Value == "dungeon")
                    dungeonCells++;
                else if (entry.Value == "desert")
                    desertCells++;
            }

            Assert.Greater(dungeonCells, 0);
            Assert.Greater(desertCells, 0);
            Assert.AreEqual(2, rebuilt.Length);
        }
    }
}
