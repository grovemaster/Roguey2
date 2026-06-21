using JRogue.World.Generation;
using NUnit.Framework;

namespace JRogue.Tests.UnitTests.World
{
    public sealed class DungeonEntryServiceTests
    {
        [Test]
        public void ResolveDungeonSceneName_TownTest_LoadsTestDungeon()
        {
            Assert.That(
                DungeonEntryService.ResolveDungeonSceneName(DungeonEntryService.TownTestSceneName),
                Is.EqualTo(DungeonEntryService.TestDungeonSceneName));
        }

        [Test]
        public void ResolveDungeonSceneName_DimensionSquareTest_LoadsProductionDungeon()
        {
            Assert.That(
                DungeonEntryService.ResolveDungeonSceneName("DimensionSquareTest"),
                Is.EqualTo(DungeonEntryService.ProductionDungeonSceneName));
        }

        [Test]
        public void ResolveDungeonSceneName_DistrictTownTest_LoadsProductionDungeon()
        {
            Assert.That(
                DungeonEntryService.ResolveDungeonSceneName("DistrictTownTest"),
                Is.EqualTo(DungeonEntryService.ProductionDungeonSceneName));
        }

        [Test]
        public void ResolveDungeonSceneName_NullOrEmpty_LoadsProductionDungeon()
        {
            Assert.That(
                DungeonEntryService.ResolveDungeonSceneName(null),
                Is.EqualTo(DungeonEntryService.ProductionDungeonSceneName));
            Assert.That(
                DungeonEntryService.ResolveDungeonSceneName(string.Empty),
                Is.EqualTo(DungeonEntryService.ProductionDungeonSceneName));
        }
    }
}
