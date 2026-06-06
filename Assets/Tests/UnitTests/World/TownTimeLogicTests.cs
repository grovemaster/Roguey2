using JRogue.World.Generation;
using NUnit.Framework;

namespace JRogue.Tests.UnitTests.World
{
    [TestFixture]
    public sealed class TownTimeLogicTests
    {
        [Test]
        public void IsDungeonPortalOpen_OnlyOnDaysOneFourSevenMorning()
        {
            Assert.IsTrue(TownTimeLogic.IsDungeonPortalOpen(1, TownTimePhase.Morning));
            Assert.IsFalse(TownTimeLogic.IsDungeonPortalOpen(1, TownTimePhase.Day));
            Assert.IsFalse(TownTimeLogic.IsDungeonPortalOpen(2, TownTimePhase.Morning));
            Assert.IsTrue(TownTimeLogic.IsDungeonPortalOpen(4, TownTimePhase.Morning));
            Assert.IsTrue(TownTimeLogic.IsDungeonPortalOpen(7, TownTimePhase.Morning));
        }

        [Test]
        public void AdvancePhase_CyclesMorningDayNightThenIncrementsDay()
        {
            int day = 1;
            var phase = TownTimePhase.Morning;

            TownTimeAdvanceResult toDay = TownTimeLogic.AdvancePhase(day, phase, out day, out phase);
            Assert.IsTrue(toDay.Advanced);
            Assert.IsTrue(toDay.PortalWindowClosed);
            Assert.AreEqual(TownTimePhase.Day, phase);
            Assert.AreEqual(1, day);

            TownTimeAdvanceResult toNight = TownTimeLogic.AdvancePhase(day, phase, out day, out phase);
            Assert.IsFalse(toNight.PortalWindowClosed);
            Assert.AreEqual(TownTimePhase.Night, phase);
            Assert.AreEqual(1, day);

            TownTimeAdvanceResult toNextMorning = TownTimeLogic.AdvancePhase(day, phase, out day, out phase);
            Assert.IsTrue(toNextMorning.CalendarDayChanged);
            Assert.AreEqual(TownTimePhase.Morning, phase);
            Assert.AreEqual(2, day);
        }

        [Test]
        public void BuildPortalClosedMessage_UsesPhaseAndDay()
        {
            string wrongPhase = TownTimeLogic.BuildPortalClosedMessage(1, TownTimePhase.Day);
            StringAssert.Contains("morning", wrongPhase.ToLowerInvariant());

            string wrongDay = TownTimeLogic.BuildPortalClosedMessage(2, TownTimePhase.Morning);
            StringAssert.Contains("day 2", wrongDay);
            StringAssert.Contains("1, 4, 7", wrongDay);
        }
    }
}
