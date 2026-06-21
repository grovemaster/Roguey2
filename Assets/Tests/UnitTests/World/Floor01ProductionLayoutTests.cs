using System.Collections.Generic;
using JRogue.World.Generation.Zones;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.World
{
    [TestFixture]
    public sealed class Floor01ProductionLayoutTests
    {
        [Test]
        public void ProductionNormalizedRects_Partition50x80_Into60And20Rows()
        {
            var centerRect = new NormalizedRect { xMin = 0f, yMin = 0f, xMax = 1f, yMax = 0.75f };
            var northRect = new NormalizedRect { xMin = 0f, yMin = 0.75f, xMax = 1f, yMax = 1f };

            RectInt center = ZoneCompassRectResolver.ResolveRect(centerRect, 50, 80);
            RectInt north = ZoneCompassRectResolver.ResolveRect(northRect, 50, 80);

            Assert.AreEqual(50, center.width);
            Assert.AreEqual(60, center.height);
            Assert.AreEqual(0, center.xMin);
            Assert.AreEqual(0, center.yMin);

            Assert.AreEqual(50, north.width);
            Assert.AreEqual(20, north.height);
            Assert.AreEqual(0, north.xMin);
            Assert.AreEqual(60, north.yMin);

            Assert.AreEqual(center.yMax, north.yMin);
            Assert.IsFalse(ZoneCompassRectResolver.RectsOverlap(center, north));
        }

        [Test]
        public void ResolveInterfaces_ProductionPieces_IncludeCenterNorthCorridorEdge()
        {
            var centerRect = new NormalizedRect { xMin = 0f, yMin = 0f, xMax = 1f, yMax = 0.75f };
            var northRect = new NormalizedRect { xMin = 0f, yMin = 0.75f, xMax = 1f, yMax = 1f };
            var pieces = new[]
            {
                new ResolvedZonePiece(
                    "center",
                    "luminescent_cavern",
                    ZoneCompassRectResolver.ResolveRect(centerRect, 50, 80),
                    true),
                new ResolvedZonePiece(
                    "north",
                    "northern_dark",
                    ZoneCompassRectResolver.ResolveRect(northRect, 50, 80),
                    false),
            };

            List<ZoneInterface> interfaces = ZoneInterfaceResolver.ResolveInterfaces(pieces, 50, 80);
            bool found = false;
            for (int i = 0; i < interfaces.Count; i++)
            {
                ZoneInterface iface = interfaces[i];
                if (iface.PieceAId == "center"
                    && iface.PieceBId == "north"
                    && iface.EdgeOnA == ZoneInterfaceEdge.North)
                {
                    found = true;
                    Assert.AreEqual(50, iface.SpanMax - iface.SpanMin);
                    break;
                }
            }

            Assert.IsTrue(found, "Expected shared center→north interface spanning full floor width.");
        }
    }

    [TestFixture]
    public sealed class ZoneBoundaryOpeningPlannerTests
    {
        [Test]
        public void RollOpeningWidths_ThreeEntrances_StayWithin1To3()
        {
            var rng = new System.Random(12345);
            int[] widths = ZoneBoundaryOpeningPlanner.RollOpeningWidths(3, 1, 3, rng);

            Assert.AreEqual(3, widths.Length);
            for (int i = 0; i < widths.Length; i++)
            {
                Assert.GreaterOrEqual(widths[i], 1);
                Assert.LessOrEqual(widths[i], 3);
            }
        }

        [Test]
        public void BuildOpeningMask_ThreeEntrancesOn50WideEdge_OpensExactlyThreeRegions()
        {
            int[] widths = { 2, 1, 3 };
            bool[] mask = ZoneBoundaryOpeningPlanner.BuildOpeningMask(0, 50, ZoneBoundaryKind.Corridor, widths);

            Assert.NotNull(mask);
            Assert.AreEqual(50, mask.Length);

            int openRuns = 0;
            bool inRun = false;
            for (int i = 0; i < mask.Length; i++)
            {
                if (mask[i] && !inRun)
                {
                    openRuns++;
                    inRun = true;
                }
                else if (!mask[i])
                {
                    inRun = false;
                }
            }

            Assert.AreEqual(3, openRuns);
            Assert.AreEqual(6, CountOpen(mask));
        }

        [Test]
        public void ResolveCorridorParams_CenterNorthEdge_RollsThreeVariableWidths()
        {
            var layout = ScriptableObject.CreateInstance<DungeonFloorZoneLayout>();
            layout.ReplaceAuthoringData(
                50,
                80,
                ZoneLayoutKind.CompassSlots,
                ZoneIds.Rock,
                new ZoneSelectionRule[0],
                new[]
                {
                    new ZoneLayoutPiece
                    {
                        pieceId = "center",
                        edgeBoundaries = new[]
                        {
                            new ZoneEdgeBoundary
                            {
                                neighborPieceId = "north",
                                boundaryKind = ZoneBoundaryKind.Corridor,
                                corridorCount = 3,
                                corridorWidthMin = 1,
                                corridorWidthMax = 3,
                            },
                        },
                    },
                });

            var iface = new ZoneInterface("center", "north", ZoneInterfaceEdge.North, 0, 50, 59);
            var rng = new System.Random(99);

            List<ResolvedZoneBoundary> resolved = ZoneBoundaryResolver.ResolveAll(
                layout,
                new[]
                {
                    new ResolvedZonePiece("center", "luminescent_cavern", new RectInt(0, 0, 50, 60), true),
                    new ResolvedZonePiece("north", "northern_dark", new RectInt(0, 60, 50, 20), false),
                },
                new List<ZoneInterface> { iface },
                rng);

            Assert.AreEqual(1, resolved.Count);
            Assert.AreEqual(3, resolved[0].OpeningWidths.Length);
            Assert.AreEqual(ZoneBoundaryKind.Corridor, resolved[0].Kind);

            Object.DestroyImmediate(layout);
        }

        static int CountOpen(bool[] mask)
        {
            int count = 0;
            for (int i = 0; i < mask.Length; i++)
            {
                if (mask[i])
                    count++;
            }

            return count;
        }
    }
}
