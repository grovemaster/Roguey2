using JRogue.Item.Essence;
using JRogue.Manager.Floor;
using JRogue.Tests.UnitTests.Input;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.Essence
{
    [TestFixture]
    public class FloorEssenceLifetimeTests
    {
        GameObject _serviceGo;

        [SetUp]
        public void SetUp()
        {
            InputTestSceneBuilder.ResetSingletonManagersForTests();
            _serviceGo = new GameObject("FloorEssenceService");
            _serviceGo.AddComponent<FloorEssenceService>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_serviceGo != null)
                Object.DestroyImmediate(_serviceGo);

            InputTestSceneBuilder.ResetSingletonManagersForTests();
        }

        [Test]
        public void TickDespawn_RemovesAfterPhasesExpire()
        {
            var essence = ScriptableObject.CreateInstance<EssenceData>();
            essence.essenceName = "Sudden Strength";
            essence.floorLifetimePlayerPhases = 2;

            Vector3Int tile = new Vector3Int(4, 5, 0);
            FloorEssenceService.Instance.SpawnEssence(tile, essence);
            Assert.IsTrue(FloorEssenceService.Instance.HasEssenceAt(tile));

            FloorEssenceService.Instance.TickDespawnAll();
            Assert.IsTrue(FloorEssenceService.Instance.HasEssenceAt(tile));

            FloorEssenceService.Instance.TickDespawnAll();
            Assert.IsFalse(FloorEssenceService.Instance.HasEssenceAt(tile));

            Object.DestroyImmediate(essence);
        }
    }
}
