using JRogue.World.Lighting;
using NUnit.Framework;

namespace JRogue.Tests.UnitTests.World
{
    [TestFixture]
    public sealed class IlluminationVisibilityLogicTests
    {
        const int Threshold = 3;

        [Test]
        public void IsCellLiveVisible_PartyOccupied_AlwaysTrue()
        {
            Assert.IsTrue(IlluminationVisibilityLogic.IsCellLiveVisible(0, 0, partyOccupied: true));
            Assert.IsTrue(IlluminationVisibilityLogic.IsCellLiveVisible(6, 10, partyOccupied: true));
        }

        [Test]
        public void IsCellLiveVisible_Emitter_AlwaysTrue()
        {
            Assert.IsTrue(IlluminationVisibilityLogic.IsCellLiveVisible(emitLight: 6, receivedLight: 0, partyOccupied: false));
        }

        [Test]
        public void IsCellLiveVisible_ZeroReceivedLight_NotLiveVisible()
        {
            Assert.IsFalse(IlluminationVisibilityLogic.IsCellLiveVisible(0, 0, partyOccupied: false));
        }

        [Test]
        public void IsCellLiveVisible_PositiveReceivedLight_LiveVisible()
        {
            Assert.IsTrue(IlluminationVisibilityLogic.IsCellLiveVisible(0, 1, partyOccupied: false));
            Assert.IsTrue(IlluminationVisibilityLogic.IsCellLiveVisible(0, 2, partyOccupied: false));
        }

        [Test]
        public void IsCellLiveVisible_UnlitWall_NotLiveVisibleWithoutLight()
        {
            Assert.IsFalse(IlluminationVisibilityLogic.IsCellLiveVisible(0, 0, partyOccupied: false));
        }

        [Test]
        public void IsCellFullyBright_DimReceivedLight_IsDarkTile()
        {
            Assert.IsFalse(IlluminationVisibilityLogic.IsCellFullyBright(0, 1, false, Threshold));
            Assert.IsFalse(IlluminationVisibilityLogic.IsCellFullyBright(0, 2, false, Threshold));
        }

        [Test]
        public void IsCellFullyBright_AtThreshold_IsFullyBright()
        {
            Assert.IsTrue(IlluminationVisibilityLogic.IsCellFullyBright(0, Threshold, false, Threshold));
        }

        [Test]
        public void IsCellFullyBright_Emitter_AlwaysFullyBright()
        {
            Assert.IsTrue(IlluminationVisibilityLogic.IsCellFullyBright(6, 0, false, Threshold));
        }
    }
}
