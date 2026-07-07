using System.Collections.Generic;
using JRogue.World.Generation;
using JRogue.World.Generation.Vaults;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.World
{
    [TestFixture]
    public sealed class VaultPlacementLogicTests
    {
        [Test]
        public void RollPondCount_IsDeterministicForSeed()
        {
            int a = Floor01PondPlacementLogic.RollPondCount(42, "dungeon_floor_01");
            int b = Floor01PondPlacementLogic.RollPondCount(42, "dungeon_floor_01");
            Assert.AreEqual(a, b);
            Assert.GreaterOrEqual(a, Floor01PondPlacementLogic.MinimumPondCount);
            Assert.LessOrEqual(a, Floor01PondPlacementLogic.HardCapPondCount);
        }

        [Test]
        public void RollPondCount_NeverBelowMinimum()
        {
            for (int seed = 0; seed < 200; seed++)
            {
                int count = Floor01PondPlacementLogic.RollPondCount(seed, "dungeon_floor_01");
                Assert.GreaterOrEqual(count, Floor01PondPlacementLogic.MinimumPondCount);
                Assert.LessOrEqual(count, Floor01PondPlacementLogic.HardCapPondCount);
            }
        }

        [Test]
        public void TryGetZoneGeographicCenter_ReturnsBandCenterForFloor1Layout()
        {
            var context = new DungeonGenerationContext(null, null, runSeed: 1, floorSalt: 0)
            {
                ZoneBoundsByZoneId = new Dictionary<string, RectInt>
                {
                    ["luminescent_cavern"] = new RectInt(0, 0, 50, 60),
                    ["northern_dark"] = new RectInt(0, 60, 50, 20),
                },
            };

            Assert.IsTrue(
                VaultPlacementUtility.TryGetZoneGeographicCenter(
                    context,
                    "luminescent_cavern",
                    out Vector3Int center));
            Assert.AreEqual(new Vector3Int(25, 30, 0), center);
        }

        [Test]
        public void TryResolveZoneCenterOrigin_UsesBlueprintOriginAnchor()
        {
            var blueprint = new VaultBlueprint
            {
                VaultId = "vault_monument_8x8",
                Origin = new Vector2Int(3, 3),
            };

            var context = new DungeonGenerationContext(null, null, runSeed: 1, floorSalt: 0)
            {
                ZoneBoundsByZoneId = new Dictionary<string, RectInt>
                {
                    ["luminescent_cavern"] = new RectInt(0, 0, 50, 60),
                },
            };

            Assert.IsTrue(
                VaultPlacementUtility.TryResolveZoneCenterOrigin(
                    context,
                    blueprint,
                    "luminescent_cavern",
                    out Vector3Int origin));
            Assert.AreEqual(new Vector3Int(25, 30, 0), origin);
        }

        [Test]
        public void IsWithinInteriorBounds_RejectsMapPerimeterCells()
        {
            const int width = 10;
            const int height = 10;

            Assert.IsFalse(VaultPlacementUtility.IsWithinInteriorBounds(new Vector3Int(0, 5, 0), width, height));
            Assert.IsFalse(VaultPlacementUtility.IsWithinInteriorBounds(new Vector3Int(5, 0, 0), width, height));
            Assert.IsFalse(VaultPlacementUtility.IsWithinInteriorBounds(new Vector3Int(9, 5, 0), width, height));
            Assert.IsFalse(VaultPlacementUtility.IsWithinInteriorBounds(new Vector3Int(5, 9, 0), width, height));
            Assert.IsTrue(VaultPlacementUtility.IsWithinInteriorBounds(new Vector3Int(1, 1, 0), width, height));
            Assert.IsTrue(VaultPlacementUtility.IsWithinInteriorBounds(new Vector3Int(8, 8, 0), width, height));
        }

        [Test]
        public void IsNearNorthMapEdge_UsesChebyshevDistanceToNorthRow()
        {
            Assert.IsTrue(DescentPlinthPlacementLogic.IsNearNorthMapEdge(
                new Vector3Int(10, 76, 0),
                DescentPlinthPlacementLogic.Floor01NorthMapRow,
                DescentPlinthPlacementLogic.MaxChebyshevFromNorthEdge));
            Assert.IsFalse(DescentPlinthPlacementLogic.IsNearNorthMapEdge(
                new Vector3Int(10, 75, 0),
                DescentPlinthPlacementLogic.Floor01NorthMapRow,
                DescentPlinthPlacementLogic.MaxChebyshevFromNorthEdge));
        }

        [Test]
        public void OnPlaced_StoresReturnArrivalBindingSouthOfPlinth()
        {
            var blueprint = new VaultBlueprint
            {
                VaultId = DescentPlinthPlacementLogic.VaultId,
                Origin = new Vector2Int(1, 1),
            };
            blueprint.AddInteractable("bump_descent_plinth", 1, 1);

            var instanceGo = new GameObject("FloorInstance");
            var instance = instanceGo.AddComponent<DungeonFloorInstance>();
            var context = new DungeonGenerationContext(null, instance, runSeed: 1, floorSalt: 0);
            Vector3Int origin = new Vector3Int(20, 77, 0);

            DescentPlinthPlacementLogic.OnPlaced(context, blueprint, origin);

            Assert.AreEqual(new Vector3Int(20, 77, 0), context.DescentPlinthPortalCell);
            Assert.IsTrue(instance.TryGetArrivalBinding(
                DungeonFloorTransitionIds.Floor02ToFloor01,
                out PortalArrivalBinding binding));
            Assert.AreEqual(new Vector3Int(20, 76, 0), binding.arrivalAnchor);

            Object.DestroyImmediate(instanceGo);
        }

        [Test]
        public void LocalToWorld_OriginAnchorMapsMonumentCornerToPlacementOrigin()
        {
            var blueprint = new VaultBlueprint { Origin = new Vector2Int(3, 3) };
            Vector3Int placementOrigin = new Vector3Int(25, 30, 0);
            Vector3Int monumentCorner = blueprint.LocalToWorld(placementOrigin, 3, 3);
            Assert.AreEqual(placementOrigin, monumentCorner);
        }
    }
}
