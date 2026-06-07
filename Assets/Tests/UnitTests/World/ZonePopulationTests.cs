using System.Collections.Generic;
using System.Reflection;
using JRogue.Manager.Map;
using JRogue.Tests.UnitTests.Input;
using JRogue.World.Generation;
using JRogue.World.Generation.Phases;
using JRogue.World.Generation.Zones;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.Tests.UnitTests.World
{
    [TestFixture]
    public sealed class ZonePopulationUtilityTests
    {
        readonly List<Object> _assets = new List<Object>();
        readonly List<GameObject> _objects = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            InputTestSceneBuilder.ResetSingletonManagersForTests();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] != null)
                    Object.DestroyImmediate(_objects[i]);
            }

            _objects.Clear();

            for (int i = 0; i < _assets.Count; i++)
            {
                if (_assets[i] != null)
                    Object.DestroyImmediate(_assets[i]);
            }

            _assets.Clear();
            InputTestSceneBuilder.ResetSingletonManagersForTests();
        }

        [Test]
        public void ResolveEnemyEntries_UsesAuthoritativeProfileWhenAssigned()
        {
            DungeonZonePopulationProfile profile = CreateProfile(enemies: 1, items: 0);
            DungeonFloorZoneLayout layout = CreateLayoutWithZone("dungeon", profile);
            DungeonFloorDefinition floorDef = CreateFloorDef(enemyCount: 2, fallback: true);

            IReadOnlyList<ZoneEnemyPopulationEntry> entries =
                ZonePopulationUtility.ResolveEnemyEntries(floorDef, layout, "dungeon");

            Assert.AreEqual(1, entries.Count);
            Assert.AreEqual(2, entries[0].minCount);
            Assert.AreEqual(4, entries[0].maxCount);
        }

        [Test]
        public void ResolveEnemyEntries_EmptyProfileSection_DoesNotFallbackWhenProfileAssigned()
        {
            DungeonZonePopulationProfile profile = CreateProfile(enemies: 0, items: 0);
            DungeonFloorZoneLayout layout = CreateLayoutWithZone("desert", profile);
            DungeonFloorDefinition floorDef = CreateFloorDef(enemyCount: 2, fallback: true);

            IReadOnlyList<ZoneEnemyPopulationEntry> entries =
                ZonePopulationUtility.ResolveEnemyEntries(floorDef, layout, "desert");

            Assert.AreEqual(0, entries.Count);
        }

        [Test]
        public void ResolveEnemyEntries_FallsBackToFloorWhenProfileMissing()
        {
            DungeonFloorZoneLayout layout = CreateLayoutWithZone("dungeon", profile: null);
            DungeonFloorDefinition floorDef = CreateFloorDef(enemyCount: 1, fallback: true);

            IReadOnlyList<ZoneEnemyPopulationEntry> entries =
                ZonePopulationUtility.ResolveEnemyEntries(floorDef, layout, "dungeon");

            Assert.AreEqual(1, entries.Count);
            Assert.AreEqual(0, entries[0].minCount);
            Assert.AreEqual(2, entries[0].maxCount);
        }

        [Test]
        public void ResolveFloorItemEntries_EmptyProfileSection_DoesNotFallbackWhenProfileAssigned()
        {
            DungeonZonePopulationProfile profile = CreateProfile(enemies: 1, items: 0);
            DungeonFloorZoneLayout layout = CreateLayoutWithZone("snow", profile);
            DungeonFloorDefinition floorDef = CreateFloorDef(enemyCount: 1, fallback: true);

            IReadOnlyList<ZoneFloorItemPopulationEntry> entries =
                ZonePopulationUtility.ResolveFloorItemEntries(floorDef, layout, "snow");

            Assert.AreEqual(0, entries.Count);
        }

        [Test]
        public void ResolveHazardEntries_UsesDungeonProfileAndSilencesSnow()
        {
            DungeonZonePopulationProfile dungeonProfile = CreateProfileWithHazards(hazardRows: 1);
            DungeonZonePopulationProfile snowProfile = CreateProfile(enemies: 1, items: 0);
            DungeonFloorZoneLayout layout = CreateLayoutWithZones(
                ("dungeon", dungeonProfile),
                ("snow", snowProfile));
            DungeonFloorDefinition floorDef = CreateFloorDefWithHazards(hazardCount: 1, fallback: true);

            IReadOnlyList<ZoneHazardPopulationEntry> dungeonEntries =
                ZonePopulationUtility.ResolveHazardEntries(floorDef, layout, "dungeon");
            IReadOnlyList<ZoneHazardPopulationEntry> snowEntries =
                ZonePopulationUtility.ResolveHazardEntries(floorDef, layout, "snow");

            Assert.AreEqual(1, dungeonEntries.Count);
            Assert.AreEqual(2, dungeonEntries[0].minCount);
            Assert.AreEqual(0, snowEntries.Count);
        }

        [Test]
        public void ResolveTrapEntries_FallsBackToFloorWhenProfileMissing()
        {
            DungeonFloorZoneLayout layout = CreateLayoutWithZone("dungeon", profile: null);
            DungeonFloorDefinition floorDef = CreateFloorDefWithTraps(trapCount: 1, fallback: true);

            IReadOnlyList<ZoneTrapPopulationEntry> entries =
                ZonePopulationUtility.ResolveTrapEntries(floorDef, layout, "dungeon");

            Assert.AreEqual(1, entries.Count);
        }

        [Test]
        public void GetHabitatInstances_SkipsEmptyAndRockPieces()
        {
            var context = new DungeonGenerationContext(null, null, 1, 0)
            {
                ResolvedZonePieces = new[]
                {
                    new ResolvedZonePiece("center", "dungeon", new RectInt(0, 0, 10, 10), true),
                    new ResolvedZonePiece("north", ZoneIds.Empty, new RectInt(0, 10, 10, 5), false),
                    new ResolvedZonePiece("east", ZoneIds.Rock, new RectInt(10, 0, 5, 10), false),
                },
            };

            IReadOnlyList<ResolvedZonePiece> habitat = ZonePopulationUtility.GetHabitatInstances(context);

            Assert.AreEqual(1, habitat.Count);
            Assert.AreEqual("center:dungeon", habitat[0].ZoneInstanceId);
        }

        [Test]
        public void CollectZoneInstanceCandidates_FiltersByBoundsAndZoneId()
        {
            var zoneCellMap = new Dictionary<Vector3Int, string>();
            for (int y = 0; y < 20; y++)
            {
                for (int x = 0; x < 21; x++)
                    zoneCellMap[new Vector3Int(x, y, 0)] = "dungeon";
            }

            for (int y = 20; y < 30; y++)
            {
                for (int x = 0; x < 30; x++)
                    zoneCellMap[new Vector3Int(x, y, 0)] = "snow";
            }

            var pieces = new[]
            {
                new ResolvedZonePiece("center", "dungeon", new RectInt(0, 0, 21, 20), true),
                new ResolvedZonePiece("north", "snow", new RectInt(0, 20, 30, 10), false),
            };

            var context = new DungeonGenerationContext(null, null, 1, 0)
            {
                MapWidth = 30,
                MapHeight = 30,
                PlayerStart = new Vector3Int(10, 5, 0),
                ZoneCellMap = zoneCellMap,
                ResolvedZonePieces = pieces,
                ZoneBoundsByInstanceId = ZoneCellMapBuilder.BuildZoneBounds(pieces),
            };
            context.BuildSafeZone(new[] { context.PlayerStart }, 5);

            MapManager map = CreateOpenFloorMap(30, 30);
            List<Vector3Int> snowCandidates = PopulationPlacementUtility.CollectZoneInstanceCandidates(
                map,
                context,
                "north:snow");
            List<Vector3Int> dungeonCandidates = PopulationPlacementUtility.CollectZoneInstanceCandidates(
                map,
                context,
                "center:dungeon");

            Assert.IsTrue(snowCandidates.Count > 0);
            Assert.IsTrue(dungeonCandidates.Count > 0);

            for (int i = 0; i < snowCandidates.Count; i++)
            {
                Vector3Int cell = snowCandidates[i];
                Assert.GreaterOrEqual(cell.y, 20);
                Assert.AreEqual("snow", zoneCellMap[cell]);
                Assert.IsFalse(context.IsInSafeZone(cell));
            }

            for (int i = 0; i < dungeonCandidates.Count; i++)
            {
                Vector3Int cell = dungeonCandidates[i];
                Assert.Less(cell.y, 20);
                Assert.AreEqual("dungeon", zoneCellMap[cell]);
            }
        }

        DungeonZonePopulationProfile CreateProfile(int enemies, int items)
        {
            var profile = ScriptableObject.CreateInstance<DungeonZonePopulationProfile>();
            _assets.Add(profile);

            var enemyEntries = new ZoneEnemyPopulationEntry[enemies];
            for (int i = 0; i < enemies; i++)
            {
                enemyEntries[i] = new ZoneEnemyPopulationEntry
                {
                    minCount = 2,
                    maxCount = 4,
                };
            }

            SetPrivateField(profile, "enemyPopulation", enemyEntries);
            SetPrivateField(profile, "floorItemPopulation", new ZoneFloorItemPopulationEntry[items]);
            return profile;
        }

        DungeonFloorZoneLayout CreateLayoutWithZone(string zoneId, DungeonZonePopulationProfile profile)
        {
            return CreateLayoutWithZones((zoneId, profile));
        }

        DungeonFloorZoneLayout CreateLayoutWithZones(
            params (string zoneId, DungeonZonePopulationProfile profile)[] zones)
        {
            var layout = ScriptableObject.CreateInstance<DungeonFloorZoneLayout>();
            _assets.Add(layout);

            var definitions = new DungeonZoneDefinition[zones.Length];
            for (int i = 0; i < zones.Length; i++)
            {
                var zoneDef = ScriptableObject.CreateInstance<DungeonZoneDefinition>();
                _assets.Add(zoneDef);
                SetPrivateField(zoneDef, "zoneId", zones[i].zoneId);
                SetPrivateField(zoneDef, "populationProfile", zones[i].profile);
                definitions[i] = zoneDef;
            }

            layout.ReplaceAuthoringData(
                30,
                30,
                ZoneLayoutKind.CompassSlots,
                ZoneIds.Rock,
                new ZoneSelectionRule[0],
                new ZoneLayoutPiece[0],
                definitions);

            return layout;
        }

        DungeonZonePopulationProfile CreateProfileWithHazards(int hazardRows)
        {
            var profile = CreateProfile(enemies: 0, items: 0);
            var hazardEntries = new ZoneHazardPopulationEntry[hazardRows];
            for (int i = 0; i < hazardRows; i++)
            {
                hazardEntries[i] = new ZoneHazardPopulationEntry
                {
                    minCount = 2,
                    maxCount = 4,
                };
            }

            SetPrivateField(profile, "hazardPopulation", hazardEntries);
            return profile;
        }

        DungeonFloorDefinition CreateFloorDefWithHazards(int hazardCount, bool fallback)
        {
            var floorDef = CreateFloorDef(enemyCount: 0, fallback: fallback);
            var hazardEntries = new HazardPopulationEntry[hazardCount];
            for (int i = 0; i < hazardCount; i++)
            {
                hazardEntries[i] = new HazardPopulationEntry
                {
                    minCount = 1,
                    maxCount = 2,
                };
            }

            SetPrivateField(floorDef, "hazardPopulation", hazardEntries);
            return floorDef;
        }

        DungeonFloorDefinition CreateFloorDefWithTraps(int trapCount, bool fallback)
        {
            var floorDef = CreateFloorDef(enemyCount: 0, fallback: fallback);
            var trapEntries = new TrapPopulationEntry[trapCount];
            for (int i = 0; i < trapCount; i++)
            {
                trapEntries[i] = new TrapPopulationEntry
                {
                    minCount = 1,
                    maxCount = 2,
                };
            }

            SetPrivateField(floorDef, "trapPopulation", trapEntries);
            return floorDef;
        }

        DungeonFloorDefinition CreateFloorDef(int enemyCount, bool fallback)
        {
            var floorDef = ScriptableObject.CreateInstance<DungeonFloorDefinition>();
            _assets.Add(floorDef);
            SetPrivateField(floorDef, "useFloorPopulationAsFallback", fallback);

            var enemyEntries = new EnemyPopulationEntry[enemyCount];
            for (int i = 0; i < enemyCount; i++)
            {
                enemyEntries[i] = new EnemyPopulationEntry
                {
                    minCount = 0,
                    maxCount = 2,
                };
            }

            SetPrivateField(floorDef, "enemyPopulation", enemyEntries);
            return floorDef;
        }

        MapManager CreateOpenFloorMap(int width, int height)
        {
            var root = new GameObject("OpenFloorMapRoot");
            _objects.Add(root);

            var floorGo = new GameObject("Floor");
            floorGo.transform.SetParent(root.transform);
            _objects.Add(floorGo);
            var floor = floorGo.AddComponent<Tilemap>();
            floorGo.AddComponent<TilemapRenderer>();

            Tile tile = ScriptableObject.CreateInstance<Tile>();
            _assets.Add(tile);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                    floor.SetTile(new Vector3Int(x, y, 0), tile);
            }

            var wallGo = new GameObject("Wall");
            wallGo.transform.SetParent(root.transform);
            _objects.Add(wallGo);
            Tilemap wall = wallGo.AddComponent<Tilemap>();

            var map = root.AddComponent<MapManager>();
            InputTestSceneBuilder.SetPrivateField(map, "floorMap", floor);
            InputTestSceneBuilder.SetPrivateField(map, "wallMap", wall);
            return map;
        }

        static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Missing field {fieldName} on {target.GetType().Name}");
            field.SetValue(target, value);
        }
    }

    [TestFixture]
    public sealed class ZonePopulationEntryRulesTests
    {
        readonly List<Object> _assets = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _assets.Count; i++)
            {
                if (_assets[i] != null)
                    Object.DestroyImmediate(_assets[i]);
            }

            _assets.Clear();
        }

        [Test]
        public void ChebyshevDistanceToAabbEdge_MeasuresInsetFromBounds()
        {
            var bounds = new RectInt(0, 0, 10, 10);

            Assert.AreEqual(0, ZonePopulationEntryRules.ChebyshevDistanceToAabbEdge(new Vector3Int(0, 0, 0), bounds));
            Assert.AreEqual(2, ZonePopulationEntryRules.ChebyshevDistanceToAabbEdge(new Vector3Int(2, 5, 0), bounds));
            Assert.AreEqual(4, ZonePopulationEntryRules.ChebyshevDistanceToAabbEdge(new Vector3Int(4, 4, 0), bounds));
        }

        [Test]
        public void MeetsTagRequirement_SkipsEntryWhenRequiredTagMissing()
        {
            DungeonFloorZoneLayout layout = CreateLayoutWithTaggedZone("desert", tags: null);

            Assert.IsFalse(ZonePopulationEntryRules.MeetsTagRequirement(layout, "desert", "outdoor"));
            Assert.IsTrue(ZonePopulationEntryRules.MeetsTagRequirement(layout, "desert", null));
        }

        [Test]
        public void MeetsTagRequirement_AllowsEntryWhenRequiredTagPresent()
        {
            DungeonFloorZoneLayout layout = CreateLayoutWithTaggedZone("desert", tags: new[] { "outdoor" });

            Assert.IsTrue(ZonePopulationEntryRules.MeetsTagRequirement(layout, "desert", "outdoor"));
        }

        [Test]
        public void RollSpawnCount_DensityModeScalesWithEligibleCells()
        {
            var rng = new System.Random(42);

            int scatter = ZonePopulationEntryRules.RollSpawnCount(
                ZonePopulationDensityMode.ScatterCount,
                2,
                4,
                100,
                rng);
            Assert.GreaterOrEqual(scatter, 2);
            Assert.LessOrEqual(scatter, 4);

            int density = ZonePopulationEntryRules.RollSpawnCount(
                ZonePopulationDensityMode.DensityPer100Tiles,
                2,
                4,
                200,
                new System.Random(42));
            Assert.AreEqual(8, density);
        }

        [Test]
        public void CountEligibleCandidates_ExcludesCellsNearZoneEdge()
        {
            var bounds = new RectInt(0, 20, 30, 10);
            var candidates = new List<Vector3Int>
            {
                new Vector3Int(0, 20, 0),
                new Vector3Int(5, 25, 0),
                new Vector3Int(10, 27, 0),
            };

            int eligible = ZonePopulationEntryRules.CountEligibleCandidates(
                candidates,
                startIndex: 0,
                bounds,
                forbiddenNearEdge: 2,
                cell => true);

            Assert.AreEqual(2, eligible);
            Assert.IsFalse(ZonePopulationEntryRules.MeetsEdgeRequirement(candidates[0], bounds, 2));
            Assert.IsTrue(ZonePopulationEntryRules.MeetsEdgeRequirement(candidates[1], bounds, 2));
        }

        DungeonFloorZoneLayout CreateLayoutWithTaggedZone(string zoneId, string[] tags)
        {
            var layout = ScriptableObject.CreateInstance<DungeonFloorZoneLayout>();
            _assets.Add(layout);

            var zoneDef = ScriptableObject.CreateInstance<DungeonZoneDefinition>();
            _assets.Add(zoneDef);

            FieldInfo zoneIdField = typeof(DungeonZoneDefinition).GetField(
                "zoneId",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo tagsField = typeof(DungeonZoneDefinition).GetField(
                "tags",
                BindingFlags.Instance | BindingFlags.NonPublic);
            zoneIdField.SetValue(zoneDef, zoneId);
            tagsField.SetValue(zoneDef, tags ?? System.Array.Empty<string>());

            layout.ReplaceAuthoringData(
                30,
                30,
                ZoneLayoutKind.CompassSlots,
                ZoneIds.Rock,
                new ZoneSelectionRule[0],
                new ZoneLayoutPiece[0],
                new[] { zoneDef });

            return layout;
        }
    }
}
