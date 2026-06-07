using System.Collections.Generic;
using JRogue.Spawn;
using JRogue.World.Generation;
using JRogue.World.Generation.MonsterSpawn;
using JRogue.World.Generation.Zones;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.World
{
    [TestFixture]
    public sealed class MonsterSpawnScheduleLogicTests
    {
        [Test]
        public void TryGetDaySchedule_ReturnsMatchingDay()
        {
            var group = new MonsterSpawnGroupDefinition
            {
                groupId = "hall_a",
                daySchedules = new[]
                {
                    new MonsterSpawnDaySchedule { dungeonDay = 1, composition = System.Array.Empty<MonsterSpawnCompositionRow>() },
                    new MonsterSpawnDaySchedule { dungeonDay = 3, composition = System.Array.Empty<MonsterSpawnCompositionRow>() },
                },
            };

            Assert.IsTrue(MonsterSpawnScheduleLogic.TryGetDaySchedule(group, 3, out MonsterSpawnDaySchedule schedule));
            Assert.AreEqual(3, schedule.dungeonDay);
            Assert.IsFalse(MonsterSpawnScheduleLogic.TryGetDaySchedule(group, 2, out _));
        }

        [Test]
        public void CountNeededRefill_NeverReturnsNegative()
        {
            Assert.AreEqual(2, MonsterSpawnScheduleLogic.CountNeededRefill(5, 3));
            Assert.AreEqual(0, MonsterSpawnScheduleLogic.CountNeededRefill(3, 3));
            Assert.AreEqual(0, MonsterSpawnScheduleLogic.CountNeededRefill(2, 5));
        }

        [Test]
        public void ShouldSkipOnceRow_LedgerOrAliveSkips()
        {
            var ledger = new HashSet<string> { "hall_a:giant" };

            Assert.IsTrue(MonsterSpawnScheduleLogic.ShouldSkipOnceRow(
                ledger, "hall_a:giant", aliveCount: 0, out bool markLedger));
            Assert.IsFalse(markLedger);

            Assert.IsTrue(MonsterSpawnScheduleLogic.ShouldSkipOnceRow(
                ledger, "hall_a:other", aliveCount: 1, out markLedger));
            Assert.IsTrue(markLedger);

            Assert.IsFalse(MonsterSpawnScheduleLogic.ShouldSkipOnceRow(
                ledger, "hall_a:other", aliveCount: 0, out markLedger));
        }

        [Test]
        public void ComputeSpawnCount_RefillUsesAliveDelta()
        {
            var row = new MonsterSpawnCompositionRow
            {
                fillPolicy = MonsterSpawnFillPolicy.RefillToTarget,
                targetCount = 3,
                spawnDefinition = ScriptableObject.CreateInstance<EnemySpawnDefinition>(),
            };

            try
            {
                int count = MonsterSpawnScheduleLogic.ComputeSpawnCount(
                    row,
                    aliveCount: 1,
                    onceLedger: null,
                    rowKey: "hall_a:0",
                    out bool skipped);

                Assert.IsFalse(skipped);
                Assert.AreEqual(2, count);
            }
            finally
            {
                Object.DestroyImmediate(row.spawnDefinition);
            }
        }

        [Test]
        public void ComputeSpawnCount_OnceRowSpawnsOnceThenSkips()
        {
            var row = new MonsterSpawnCompositionRow
            {
                fillPolicy = MonsterSpawnFillPolicy.OncePerDungeonIfAbsent,
                targetCount = 1,
                rowId = "giant_once",
                spawnDefinition = ScriptableObject.CreateInstance<EnemySpawnDefinition>(),
            };
            var ledger = new HashSet<string>();

            try
            {
                int first = MonsterSpawnScheduleLogic.ComputeSpawnCount(
                    row, 0, ledger, "boss:giant_once", out bool skippedFirst);
                Assert.IsFalse(skippedFirst);
                Assert.AreEqual(1, first);

                ledger.Add("boss:giant_once");

                int second = MonsterSpawnScheduleLogic.ComputeSpawnCount(
                    row, 0, ledger, "boss:giant_once", out bool skippedSecond);
                Assert.IsTrue(skippedSecond);
                Assert.AreEqual(0, second);
            }
            finally
            {
                Object.DestroyImmediate(row.spawnDefinition);
            }
        }

        [Test]
        public void ResolveRowId_PrefersExplicitRowId()
        {
            var row = new MonsterSpawnCompositionRow { rowId = "skeleton_refill" };
            Assert.AreEqual(
                "skeleton_refill",
                MonsterSpawnScheduleLogic.ResolveRowId("hall_a", row, compositionIndex: 0));
            Assert.AreEqual(
                "hall_b:2",
                MonsterSpawnScheduleLogic.ResolveRowId("hall_b", default, compositionIndex: 2));
        }

        [Test]
        public void MatchesSpeciesFilter_EmptyMatchesAll()
        {
            Assert.IsTrue(MonsterSpawnScheduleLogic.MatchesSpeciesFilter(null, "skeleton"));
            Assert.IsTrue(MonsterSpawnScheduleLogic.MatchesSpeciesFilter(string.Empty, "skeleton"));
            Assert.IsTrue(MonsterSpawnScheduleLogic.MatchesSpeciesFilter("skeleton", "skeleton"));
            Assert.IsFalse(MonsterSpawnScheduleLogic.MatchesSpeciesFilter("skeleton", "giant_skeleton"));
        }

        [Test]
        public void CollectGroups_FloorProfileBindsExplicitZoneInstance()
        {
            var profile = ScriptableObject.CreateInstance<MonsterSpawnScheduleProfile>();
            var floorDef = ScriptableObject.CreateInstance<DungeonFloorDefinition>();

            try
            {
                SetProfileGroups(profile, new MonsterSpawnGroupDefinition
                {
                    groupId = "hall_a",
                    areaBinding = new MonsterSpawnAreaBinding
                    {
                        kind = MonsterSpawnAreaBindingKind.ZoneInstance,
                        zoneInstanceId = "center:dungeon",
                    },
                });
                SetFloorSchedule(floorDef, profile);

                List<ResolvedMonsterSpawnGroup> groups =
                    MonsterSpawnScheduleService.CollectGroups(floorDef);

                Assert.AreEqual(1, groups.Count);
                Assert.AreEqual("hall_a", groups[0].ScopedGroupId);
                Assert.AreEqual("center:dungeon", groups[0].ZoneInstanceId);
            }
            finally
            {
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(floorDef);
            }
        }

        [Test]
        public void CollectGroups_ZoneProfileExpandsAcrossMatchingInstances()
        {
            var profile = ScriptableObject.CreateInstance<MonsterSpawnScheduleProfile>();
            var floorDef = ScriptableObject.CreateInstance<DungeonFloorDefinition>();
            var layout = ScriptableObject.CreateInstance<DungeonFloorZoneLayout>();
            var zoneDef = ScriptableObject.CreateInstance<DungeonZoneDefinition>();

            try
            {
                SetProfileGroups(profile, new MonsterSpawnGroupDefinition
                {
                    groupId = "hall_a",
                    areaBinding = new MonsterSpawnAreaBinding
                    {
                        kind = MonsterSpawnAreaBindingKind.ZoneId,
                        zoneId = "dungeon",
                    },
                });
                SetZoneSchedule(zoneDef, profile);
                SetLayoutZones(layout, zoneDef);
                SetFloorLayout(floorDef, layout);

            var instanceGo = new GameObject("floor");
            var instance = instanceGo.AddComponent<DungeonFloorInstance>();
            try
            {
                SetFloorDefinition(instance, floorDef);
                instance.MarkGenerated(
                    Vector3Int.zero,
                    arrivals: null,
                    zoneCellMap: null,
                    resolvedZonePieces: new[]
                    {
                        new ResolvedZonePiece("center", "dungeon", new RectInt(0, 0, 10, 10), false),
                        new ResolvedZonePiece("north", "snow", new RectInt(0, 10, 10, 10), false),
                        new ResolvedZonePiece("east", "dungeon", new RectInt(10, 0, 10, 10), false),
                    });

                List<ResolvedMonsterSpawnGroup> groups =
                    MonsterSpawnScheduleService.CollectGroups(instance);

                Assert.AreEqual(2, groups.Count);
                Assert.AreEqual("hall_a@center:dungeon", groups[0].ScopedGroupId);
                Assert.AreEqual("hall_a@east:dungeon", groups[1].ScopedGroupId);
            }
            finally
            {
                Object.DestroyImmediate(instanceGo);
            }
            }
            finally
            {
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(floorDef);
                Object.DestroyImmediate(layout);
                Object.DestroyImmediate(zoneDef);
            }
        }

        static void SetProfileGroups(MonsterSpawnScheduleProfile profile, params MonsterSpawnGroupDefinition[] groups)
        {
            var field = typeof(MonsterSpawnScheduleProfile).GetField(
                "groups",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(profile, groups);
        }

        static void SetFloorSchedule(DungeonFloorDefinition floorDef, MonsterSpawnScheduleProfile profile)
        {
            typeof(DungeonFloorDefinition)
                .GetField(
                    "monsterSpawnSchedule",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(floorDef, profile);
        }

        static void SetFloorLayout(DungeonFloorDefinition floorDef, DungeonFloorZoneLayout layout)
        {
            typeof(DungeonFloorDefinition)
                .GetField(
                    "zoneLayout",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(floorDef, layout);
        }

        static void SetFloorDefinition(DungeonFloorInstance instance, DungeonFloorDefinition floorDef)
        {
            typeof(DungeonFloorInstance)
                .GetField(
                    "definition",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(instance, floorDef);
        }

        static void SetZoneSchedule(DungeonZoneDefinition zoneDef, MonsterSpawnScheduleProfile profile)
        {
            var scheduleField = typeof(DungeonZoneDefinition).GetField(
                "monsterSpawnSchedule",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            scheduleField?.SetValue(zoneDef, profile);

            var modeField = typeof(DungeonZoneDefinition).GetField(
                "monsterPopulationMode",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            modeField?.SetValue(zoneDef, MonsterPopulationMode.ScheduledGroups);
        }

        static void SetLayoutZones(DungeonFloorZoneLayout layout, DungeonZoneDefinition zoneDef)
        {
            var field = typeof(DungeonFloorZoneLayout).GetField(
                "zoneDefinitions",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(layout, new[] { zoneDef });
        }
    }
}
