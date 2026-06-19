using JRogue.World.Generation;
using NUnit.Framework;

namespace JRogue.Tests.UnitTests.World
{
    [TestFixture]
    public sealed class GameCalendarLogicTests
    {
        [Test]
        public void IsDungeonPortalDay_EveryThirdDayStartingAtDayOne()
        {
            var day1 = new GameCalendarDate(330, 1, 1);
            var day2 = new GameCalendarDate(330, 1, 2);
            var day4 = new GameCalendarDate(330, 1, 4);
            var day7 = new GameCalendarDate(330, 1, 7);

            Assert.IsTrue(GameCalendarLogic.IsDungeonPortalDay(day1, 3, 1));
            Assert.IsFalse(GameCalendarLogic.IsDungeonPortalDay(day2, 3, 1));
            Assert.IsTrue(GameCalendarLogic.IsDungeonPortalDay(day4, 3, 1));
            Assert.IsTrue(GameCalendarLogic.IsDungeonPortalDay(day7, 3, 1));
        }

        [Test]
        public void IsDungeonPortalDay_RespectsConfigurableInterval()
        {
            var day1 = new GameCalendarDate(330, 1, 1);
            var day5 = new GameCalendarDate(330, 1, 5);
            var day9 = new GameCalendarDate(330, 1, 9);

            Assert.IsTrue(GameCalendarLogic.IsDungeonPortalDay(day1, 4, 1));
            Assert.IsFalse(GameCalendarLogic.IsDungeonPortalDay(day5, 4, 1));
            Assert.IsTrue(GameCalendarLogic.IsDungeonPortalDay(day9, 4, 1));
        }

        [Test]
        public void AdvanceOneDay_RollsMonthAndYear()
        {
            var endOfMonth = new GameCalendarDate(330, 1, 30);
            var nextMonth = GameCalendarLogic.AdvanceOneDay(endOfMonth);
            Assert.AreEqual(330, nextMonth.Year);
            Assert.AreEqual(2, nextMonth.Month);
            Assert.AreEqual(1, nextMonth.Day);

            var endOfYear = new GameCalendarDate(330, 12, 30);
            var nextYear = GameCalendarLogic.AdvanceOneDay(endOfYear);
            Assert.AreEqual(331, nextYear.Year);
            Assert.AreEqual(1, nextYear.Month);
            Assert.AreEqual(1, nextYear.Day);
        }

        [Test]
        public void CreateNewRunStartDate_BeginsAtYear330Month1Day1()
        {
            GameCalendarDate start = GameCalendarLogic.CreateNewRunStartDate();
            Assert.AreEqual(330, start.Year);
            Assert.AreEqual(1, start.Month);
            Assert.AreEqual(1, start.Day);
        }
    }
}
