using JRogue.World.Generation;
using NUnit.Framework;

namespace JRogue.Tests.UnitTests.World
{
    [TestFixture]
    public sealed class PortalFogVisibilityPolicyTests
    {
        [Test]
        public void DistrictPortal_hides_when_outside_fog_on_hub()
        {
            Assert.IsFalse(PortalFogVisibilityPolicy.ShouldRenderPortal(
                cellInFog: false,
                requiresTownTimeOpen: false,
                isHubFloor: true,
                portalOpen: true));
        }

        [Test]
        public void TownDungeonPortal_stays_visible_on_hub_when_outside_fog()
        {
            Assert.IsTrue(PortalFogVisibilityPolicy.ShouldRenderPortal(
                cellInFog: false,
                requiresTownTimeOpen: true,
                isHubFloor: true,
                portalOpen: true));
        }

        [Test]
        public void Portal_hides_when_closed_even_on_hub()
        {
            Assert.IsFalse(PortalFogVisibilityPolicy.ShouldRenderPortal(
                cellInFog: true,
                requiresTownTimeOpen: true,
                isHubFloor: true,
                portalOpen: false));
        }

        [Test]
        public void Portal_shows_when_in_fog()
        {
            Assert.IsTrue(PortalFogVisibilityPolicy.ShouldRenderPortal(
                cellInFog: true,
                requiresTownTimeOpen: false,
                isHubFloor: false,
                portalOpen: true));
        }
    }
}
