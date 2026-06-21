using System.Collections.Generic;
using JRogue.World.Lighting;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.World
{
    [TestFixture]
    public sealed class DarknessVisibilityLogicTests
    {
        [Test]
        public void MemberNavigatesBlind_WithoutPersonalLight_ReturnsTrue()
        {
            Assert.IsTrue(DarknessVisibilityLogic.MemberNavigatesBlind(
                zoneRequiresPersonalLight: true,
                hasPersonalVisionLight: false));
        }

        [Test]
        public void MemberNavigatesBlind_WithTorch_ReturnsFalse()
        {
            Assert.IsFalse(DarknessVisibilityLogic.MemberNavigatesBlind(
                zoneRequiresPersonalLight: true,
                hasPersonalVisionLight: true));
        }

        [Test]
        public void MemberNavigatesBlind_InLitZone_ReturnsFalse()
        {
            Assert.IsFalse(DarknessVisibilityLogic.MemberNavigatesBlind(
                zoneRequiresPersonalLight: false,
                hasPersonalVisionLight: false));
        }

        [Test]
        public void ApplyMemberVisibility_BlindInPitchDark_OnlyShowsOrigin()
        {
            var geoLos = new List<Vector3Int>
            {
                new(0, 0, 0),
                new(1, 0, 0),
                new(2, 0, 0),
                new(0, 1, 0),
            };
            var visible = new HashSet<Vector3Int>();
            var litVisible = new HashSet<Vector3Int>();

            DarknessVisibilityLogic.ApplyMemberVisibility(
                geoLos,
                new Vector3Int(0, 0, 0),
                blindInPitchDark: true,
                _ => true,
                _ => true,
                _ => true,
                visible,
                litVisible);

            Assert.AreEqual(1, visible.Count);
            Assert.IsTrue(visible.Contains(new Vector3Int(0, 0, 0)));
            Assert.IsTrue(litVisible.Contains(new Vector3Int(0, 0, 0)));
        }

        [Test]
        public void ApplyMemberVisibility_PeeringIntoDark_ShowsLitCoreAndFirstDarkTileOnly()
        {
            var geoLos = new List<Vector3Int>
            {
                new(0, 0, 0),
                new(1, 0, 0),
                new(2, 0, 0),
                new(3, 0, 0),
            };
            var visible = new HashSet<Vector3Int>();
            var litVisible = new HashSet<Vector3Int>();

            DarknessVisibilityLogic.ApplyMemberVisibility(
                geoLos,
                new Vector3Int(0, 0, 0),
                blindInPitchDark: false,
                cell => cell.x <= 1,
                cell => cell.x <= 1,
                cell => cell.x >= 2,
                visible,
                litVisible);

            Assert.IsTrue(visible.Contains(new Vector3Int(0, 0, 0)));
            Assert.IsTrue(visible.Contains(new Vector3Int(1, 0, 0)));
            Assert.IsTrue(visible.Contains(new Vector3Int(2, 0, 0)), "first pitch-dark tile should be visible");
            Assert.IsFalse(visible.Contains(new Vector3Int(3, 0, 0)), "deeper darkness should stay hidden");
        }

        [Test]
        public void CollectDarknessEdgeCells_OnlyAdjacentToLitCore()
        {
            var geoLos = new List<Vector3Int>
            {
                new(0, 0, 0),
                new(1, 0, 0),
                new(2, 0, 0),
                new(1, 1, 0),
            };
            var litCore = new HashSet<Vector3Int> { new(0, 0, 0) };
            var edge = new HashSet<Vector3Int>();

            DarknessVisibilityLogic.CollectDarknessEdgeCells(
                geoLos,
                litCore,
                cell => cell.x != 0,
                edge);

            Assert.IsTrue(edge.Contains(new Vector3Int(1, 0, 0)));
            Assert.IsTrue(edge.Contains(new Vector3Int(1, 1, 0)));
            Assert.IsFalse(edge.Contains(new Vector3Int(2, 0, 0)));
        }
    }
}
