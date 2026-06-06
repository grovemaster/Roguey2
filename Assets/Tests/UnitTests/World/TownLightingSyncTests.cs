using JRogue.World.Generation;
using JRogue.World.Lighting;
using NUnit.Framework;

namespace JRogue.Tests.UnitTests.World
{
    [TestFixture]
    public sealed class TownLightingSyncTests
    {
        [Test]
        public void AmbientLightForPhase_MatchesLockedTable()
        {
            Assert.AreEqual(8, TownLightingSync.AmbientLightForPhase(TownTimePhase.Morning));
            Assert.AreEqual(LightLevel.FullDaylightAmbient, TownLightingSync.AmbientLightForPhase(TownTimePhase.Day));
            Assert.AreEqual(LightLevel.PitchDark, TownLightingSync.AmbientLightForPhase(TownTimePhase.Night));
        }
    }
}
