using System.Collections.Generic;
using JRogue.Item;
using JRogue.World.Generation;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.UnitTests.World
{
    [TestFixture]
    public sealed class SafeZonePolicyLogicTests
    {
        readonly List<Object> _assets = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object asset in _assets)
            {
                if (asset != null)
                    Object.DestroyImmediate(asset);
            }

            _assets.Clear();
        }

        [Test]
        public void ResolvePolicyAt_UsesFloorDefaultWhenNoRegions()
        {
            FloorCombatPolicy policy = SafeZonePolicyLogic.ResolvePolicyAt(
                FloorCombatPolicy.SafeZone,
                null,
                new Vector3Int(5, 5, 0));

            Assert.AreEqual(FloorCombatPolicy.SafeZone, policy);
        }

        [Test]
        public void ResolvePolicyAt_RegionOverridesFloorDefault()
        {
            var regions = new[]
            {
                new SafeZoneRegion
                {
                    regionId = "arena",
                    minInclusive = new Vector2Int(10, 10),
                    maxInclusive = new Vector2Int(12, 12),
                    policy = FloorCombatPolicy.Normal,
                },
            };

            Assert.AreEqual(
                FloorCombatPolicy.Normal,
                SafeZonePolicyLogic.ResolvePolicyAt(FloorCombatPolicy.SafeZone, regions, new Vector3Int(11, 11, 0)));

            Assert.AreEqual(
                FloorCombatPolicy.SafeZone,
                SafeZonePolicyLogic.ResolvePolicyAt(FloorCombatPolicy.SafeZone, regions, new Vector3Int(1, 1, 0)));
        }

        [Test]
        public void ResolvePolicyAt_OverlapPrefersSmallerRegion()
        {
            var regions = new[]
            {
                new SafeZoneRegion
                {
                    regionId = "large_safe",
                    minInclusive = new Vector2Int(0, 0),
                    maxInclusive = new Vector2Int(9, 9),
                    policy = FloorCombatPolicy.SafeZone,
                },
                new SafeZoneRegion
                {
                    regionId = "small_normal",
                    minInclusive = new Vector2Int(4, 4),
                    maxInclusive = new Vector2Int(5, 5),
                    policy = FloorCombatPolicy.Normal,
                },
            };

            Assert.AreEqual(
                FloorCombatPolicy.Normal,
                SafeZonePolicyLogic.ResolvePolicyAt(FloorCombatPolicy.SafeZone, regions, new Vector3Int(4, 4, 0)));
        }

        [Test]
        public void IsUtilityInventoryUse_DoorKeyIsUtility()
        {
            var key = ScriptableObject.CreateInstance<DoorKeyItemData>();
            _assets.Add(key);
            key.targetDoorId = "door_a";

            Assert.IsTrue(SafeZonePolicyLogic.IsUtilityInventoryUse(key));
        }

        [Test]
        public void IsUtilityInventoryUse_CombatItemDeniedUnlessFlagged()
        {
            var potion = ScriptableObject.CreateInstance<ItemData>();
            _assets.Add(potion);
            potion.category = ItemCategory.Potion;

            Assert.IsFalse(SafeZonePolicyLogic.IsUtilityInventoryUse(potion));

            potion.allowUseInSafeZone = true;
            Assert.IsTrue(SafeZonePolicyLogic.IsUtilityInventoryUse(potion));
        }
    }
}
