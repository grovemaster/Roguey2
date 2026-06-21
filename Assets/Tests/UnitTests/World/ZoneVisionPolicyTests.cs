using JRogue.World.Generation.Zones;
using JRogue.World.Lighting;
using NUnit.Framework;

namespace JRogue.Tests.UnitTests.World
{
    [TestFixture]
    public sealed class ZoneVisionPolicyTests
    {
        [Test]
        public void NorthernDark_RequiresPersonalLight_WithoutTorch_IsBlind()
        {
            Assert.IsTrue(ZoneVisionPolicy.ZoneRequiresPersonalLightForVision(
                ZoneVisionPolicy.NorthernDarkZoneId,
                layout: null));
            Assert.IsTrue(DarknessVisibilityLogic.MemberNavigatesBlind(
                zoneRequiresPersonalLight: true,
                hasPersonalVisionLight: false));
        }

        [Test]
        public void NorthernDark_WithTorch_IsNotBlind()
        {
            Assert.IsFalse(DarknessVisibilityLogic.MemberNavigatesBlind(
                zoneRequiresPersonalLight: true,
                hasPersonalVisionLight: true));
        }

        [Test]
        public void IsPitchDarkForVision_NorthernDarkWithoutTorch_EvenWithReceivedLightLeak()
        {
            Assert.IsTrue(ZoneVisionPolicy.IsPitchDarkForVision(
                ZoneVisionPolicy.NorthernDarkZoneId,
                emitLight: 0,
                receivedLight: 2,
                layout: null,
                hasPersonalVisionLight: false));
        }

        [Test]
        public void ShouldSuppressFogMemory_NorthernDarkWithoutTorch()
        {
            Assert.IsTrue(ZoneVisionPolicy.ShouldSuppressFogMemory(
                ZoneVisionPolicy.NorthernDarkZoneId,
                layout: null,
                partyHasPersonalVisionLight: false));
        }
    }
}
