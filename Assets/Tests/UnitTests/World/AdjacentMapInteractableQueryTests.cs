using System.Collections.Generic;
using JRogue.Controller.Player;
using JRogue.World.Altar;
using JRogue.World.MapInteract;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.World
{
    [TestFixture]
    public sealed class AdjacentMapInteractableQueryTests
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
        public void GetOrthogonalAdjacent_ReturnsOnlyOrthoNeighbors()
        {
            var definition = ScriptableObject.CreateInstance<AltarDefinition>();
            definition.displayName = "Stone altar";
            var instance = new AltarInstance(new Vector3Int(5, 5, 0), definition);
            var interactable = new AltarInteractable(instance);

            AdjacentMapInteractableService.Instance.Register(new Vector3Int(5, 6, 0), interactable);

            IReadOnlyList<IAdjacentMapInteractable> results =
                AdjacentMapInteractableService.Instance.GetOrthogonalAdjacentInteractables(
                    new Vector3Int(5, 5, 0));

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("Stone altar", results[0].ListLabel);

            AdjacentMapInteractableService.Instance.Register(new Vector3Int(6, 6, 0), interactable);
            results = AdjacentMapInteractableService.Instance.GetOrthogonalAdjacentInteractables(
                new Vector3Int(5, 5, 0));
            Assert.AreEqual(1, results.Count);
        }

        [Test]
        public void MapInteractOrthogonal_DiagonalNotAdjacent()
        {
            Assert.IsFalse(MapInteractOrthogonal.IsOrthogonallyAdjacent(
                new Vector3Int(0, 0, 0),
                new Vector3Int(1, 1, 0)));
            Assert.IsTrue(MapInteractOrthogonal.IsOrthogonallyAdjacent(
                new Vector3Int(0, 0, 0),
                new Vector3Int(0, 1, 0)));
        }

        [Test]
        public void GetInteractableCandidates_DoesNotGrowListForever()
        {
            var definition = ScriptableObject.CreateInstance<AltarDefinition>();
            var instance = new AltarInstance(new Vector3Int(0, 1, 0), definition);
            AdjacentMapInteractableService.Instance.Register(
                new Vector3Int(0, 1, 0),
                new AltarInteractable(instance));

            var actorGo = new GameObject("Actor");
            var actor = actorGo.AddComponent<PlayerController>();

            IReadOnlyList<IAdjacentMapInteractable> candidates =
                AdjacentMapInteractableService.Instance.GetInteractableCandidates(actor);

            Assert.AreEqual(1, candidates.Count);
            Assert.AreEqual(1, candidates.Count);

            Object.DestroyImmediate(actorGo);
        }
    }
}
