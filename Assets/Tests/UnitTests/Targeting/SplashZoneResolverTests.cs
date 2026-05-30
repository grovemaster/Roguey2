using System.Collections.Generic;
using JRogue.Core.Actor;
using JRogue.Core.Targeting;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.Targeting
{
    [TestFixture]
    public sealed class SplashZoneResolverTests
    {
        [Test]
        public void DiskChebyshev_Radius2_Has25EffectCells_24SplashPreview()
        {
            SplashZoneDefinition zone = ScriptableObject.CreateInstance<SplashZoneDefinition>();
            zone.shapeKind = SplashZoneShapeKind.DiskChebyshev;
            zone.radius = 2;

            var ctx = new SplashZoneContext(Vector3Int.zero, new Vector3Int(3, 3, 0), FacingDirection.North);

            IReadOnlyList<Vector3Int> effect = SplashZoneResolver.GetEffectCells(zone, ctx);
            IReadOnlyList<Vector3Int> splash = SplashZoneResolver.GetSplashPreviewCells(zone, ctx);

            Assert.AreEqual(25, effect.Count);
            Assert.AreEqual(24, splash.Count);
            Assert.IsFalse(Contains(splash, ctx.PrimaryTile));
            Assert.IsTrue(Contains(effect, ctx.PrimaryTile));
        }

        [Test]
        public void None_OnlyPrimaryInEffect_NoSplashPreview()
        {
            SplashZoneDefinition zone = ScriptableObject.CreateInstance<SplashZoneDefinition>();
            zone.shapeKind = SplashZoneShapeKind.None;

            var ctx = new SplashZoneContext(Vector3Int.zero, new Vector3Int(2, 2, 0), FacingDirection.North);

            IReadOnlyList<Vector3Int> effect = SplashZoneResolver.GetEffectCells(zone, ctx);
            IReadOnlyList<Vector3Int> splash = SplashZoneResolver.GetSplashPreviewCells(zone, ctx);

            Assert.AreEqual(1, effect.Count);
            Assert.AreEqual(ctx.PrimaryTile, effect[0]);
            Assert.AreEqual(0, splash.Count);
        }

        [Test]
        public void LineFromCaster_ExcludesCasterAndPrimary()
        {
            SplashZoneDefinition zone = ScriptableObject.CreateInstance<SplashZoneDefinition>();
            zone.shapeKind = SplashZoneShapeKind.LineFromCaster;
            zone.maxLength = 5;

            var ctx = new SplashZoneContext(new Vector3Int(0, 0, 0), new Vector3Int(0, 4, 0), FacingDirection.North);

            IReadOnlyList<Vector3Int> splash = SplashZoneResolver.GetSplashPreviewCells(zone, ctx);

            Assert.AreEqual(3, splash.Count);
            Assert.IsTrue(Contains(splash, new Vector3Int(0, 1, 0)));
            Assert.IsTrue(Contains(splash, new Vector3Int(0, 2, 0)));
            Assert.IsTrue(Contains(splash, new Vector3Int(0, 3, 0)));
            Assert.IsFalse(Contains(splash, ctx.CasterCell));
            Assert.IsFalse(Contains(splash, ctx.PrimaryTile));
        }

        static bool Contains(IReadOnlyList<Vector3Int> cells, Vector3Int cell)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i] == cell)
                    return true;
            }

            return false;
        }
    }
}
