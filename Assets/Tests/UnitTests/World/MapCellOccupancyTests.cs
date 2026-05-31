using JRogue.GridFeatures;
using JRogue.World.Altar;
using JRogue.World.MapInteract;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.World
{
    [TestFixture]
    public sealed class MapCellOccupancyTests
    {
        GameObject _serviceGo;

        [SetUp]
        public void SetUp()
        {
            _serviceGo = new GameObject("AdjacentMapInteractableService");
            _serviceGo.AddComponent<AdjacentMapInteractableService>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_serviceGo != null)
                Object.DestroyImmediate(_serviceGo);
        }

        [Test]
        public void BlocksActorEntry_AltarCellWithBlocksOccupancy_ReturnsTrue()
        {
            var definition = ScriptableObject.CreateInstance<AltarDefinition>();
            definition.blocksOccupancy = true;
            var instance = new AltarInstance(new Vector3Int(3, 3, 0), definition);

            AdjacentMapInteractableService.Instance.Register(
                instance.Cell,
                new AltarInteractable(instance));

            Assert.IsTrue(MapCellOccupancy.BlocksActorEntry(new Vector3Int(3, 3, 0)));
            Assert.IsFalse(MapCellOccupancy.BlocksActorEntry(new Vector3Int(3, 4, 0)));
        }
    }
}
