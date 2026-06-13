using JRogue.World.Generation;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.World
{
    [TestFixture]
    public sealed class TownPlazaMarkerLayoutTests
    {
        [Test]
        public void ValidateUniqueCells_AllMarkersOnDistinctTiles()
        {
            Assert.IsTrue(
                TownPlazaMarkerLayout.ValidateUniqueCells(out string error),
                error);
        }

        [Test]
        public void MageTutorAndArcaneVendor_DoNotShareTilesWithShrinesOrForgemaster()
        {
            Assert.IsTrue(
                TownPlazaMarkerLayout.TryGetCell(StampMarkerIds.MageTutor, out Vector3Int mageTutor));
            Assert.IsTrue(
                TownPlazaMarkerLayout.TryGetCell(StampMarkerIds.ArcaneVendor, out Vector3Int arcaneVendor));
            Assert.IsTrue(
                TownPlazaMarkerLayout.TryGetCell(StampMarkerIds.MeditationShrine, out Vector3Int shrine));
            Assert.IsTrue(
                TownPlazaMarkerLayout.TryGetCell(StampMarkerIds.FleshmetalForgemaster, out Vector3Int forgemaster));

            Assert.AreNotEqual(mageTutor, shrine);
            Assert.AreNotEqual(arcaneVendor, forgemaster);
            Assert.AreNotEqual(mageTutor, arcaneVendor);
        }
    }
}
