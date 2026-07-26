using JRogue.World.Rift;
using NUnit.Framework;

namespace JRogue.Tests.UnitTests.World
{
    public sealed class RiftPortalGateLogicTests
    {
        [Test]
        public void PlayerTrigger_RequiresMinDay()
        {
            Assert.IsFalse(RiftPortalGateLogic.PassesPlayerTrigger(
                hostHasRifts: true,
                dungeonDay: 1,
                minDungeonDay: 2,
                portalAlreadyUsedThisRun: false,
                currentRunIndex: 1,
                lastPortalOpenedRunIndex: 0,
                minRunsBetweenPortals: 3,
                out string deny));
            Assert.That(deny, Does.Contain("day"));
        }

        [Test]
        public void PlayerTrigger_BlocksSameRunReuse()
        {
            Assert.IsFalse(RiftPortalGateLogic.PassesPlayerTrigger(
                true, 2, 2, portalAlreadyUsedThisRun: true, 1, 0, 3, out _));
        }

        [Test]
        public void PlayerTrigger_Cooldown_NextEligibleIsLastPlusFour()
        {
            // last=1, minBetween=3 → eligible on run 5
            Assert.AreEqual(5, RiftPortalGateLogic.NextEligibleRunAfterPortal(1, 3));

            Assert.IsFalse(RiftPortalGateLogic.PassesPlayerTrigger(
                true, 2, 2, false, currentRunIndex: 4, lastPortalOpenedRunIndex: 1, 3, out _));
            Assert.IsTrue(RiftPortalGateLogic.PassesPlayerTrigger(
                true, 2, 2, false, currentRunIndex: 5, lastPortalOpenedRunIndex: 1, 3, out _));
        }

        [Test]
        public void PlayerTrigger_NeverOpened_AllowsWhenDayOk()
        {
            Assert.IsTrue(RiftPortalGateLogic.PassesPlayerTrigger(
                true, 2, 2, false, 1, lastPortalOpenedRunIndex: 0, 3, out _));
        }

        [Test]
        public void Wandering_RequiresRunsWithoutEntry()
        {
            // Never entered: need run index >= 6 when minBefore=5
            Assert.IsFalse(RiftPortalGateLogic.PassesWandering(
                true, 2, 2, false, currentRunIndex: 5, lastRiftEnteredRunIndex: 0, 5, out _));
            Assert.IsTrue(RiftPortalGateLogic.PassesWandering(
                true, 2, 2, false, currentRunIndex: 6, lastRiftEnteredRunIndex: 0, 5, out _));
        }

        [Test]
        public void Wandering_CountsFromLastEntry()
        {
            // Entered on run 2 → wandering from run 2+5+1 = 8
            Assert.IsFalse(RiftPortalGateLogic.PassesWandering(
                true, 2, 2, false, currentRunIndex: 7, lastRiftEnteredRunIndex: 2, 5, out _));
            Assert.IsTrue(RiftPortalGateLogic.PassesWandering(
                true, 2, 2, false, currentRunIndex: 8, lastRiftEnteredRunIndex: 2, 5, out _));
        }

        [Test]
        public void NoRifts_AlwaysDenied()
        {
            Assert.IsFalse(RiftPortalGateLogic.PassesPlayerTrigger(
                false, 9, 2, false, 9, 0, 3, out _));
            Assert.IsFalse(RiftPortalGateLogic.PassesWandering(
                false, 9, 2, false, 9, 0, 5, out _));
        }
    }
}
