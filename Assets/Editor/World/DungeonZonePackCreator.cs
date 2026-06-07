#if UNITY_EDITOR
using System.IO;
using JRogue.Hazards;
using JRogue.Interactables;
using JRogue.Item;
using JRogue.Spawn;
using JRogue.Traps;
using JRogue.World.Generation;
using JRogue.World.Generation.Zones;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JRogue.Editor.World
{
    public static class DungeonZonePackCreator
    {
        const string ZoneRoot = "Assets/Data/Dungeon/Zones";
        const string LayoutRoot = "Assets/Data/Dungeon/Layouts";
        const string FloorTilePath = "Assets/TileMaps/Scavengers_SpriteSheet_32.asset";
        const string WallTilePath = "Assets/TileMaps/Scavengers_SpriteSheet_50.asset";
        const string SandFloorPath = "Assets/TileMaps/Vault/SandTheme_32.asset";
        const string SandWallPath = "Assets/TileMaps/Vault/SandTheme_50.asset";
        const string SnowFloorPath = "Assets/TileMaps/Vault/SnowTheme_32.asset";
        const string SnowWallPath = "Assets/TileMaps/Vault/SnowTheme_48.asset";
        const string Floor01DefPath = "Assets/Resources/Dungeon/Floor_dungeon_floor_01.asset";
        const string Floor03DefPath = "Assets/Resources/Dungeon/Floor_dungeon_floor_03.asset";
        const string Floor01LayoutPath = LayoutRoot + "/Layout_Floor01_Zones.asset";
        const string Floor03LayoutPath = LayoutRoot + "/Layout_Floor03_Zones.asset";
        const string PopulationRoot = ZoneRoot + "/Population";
        const string DungeonSubStampPath = "Assets/Resources/Dungeon/Stamp_Floor02_20x20.asset";
        const string SkeletonSpawnPath = "Assets/Resources/Dungeon/Spawn_DungeonTestSkeleton.asset";
        const string HandheldTorchPath = "Assets/Resources/Item/Accessory/Accessory_HandheldTorch.asset";
        const string LavaHazardPath = "Assets/Resources/Hazards/EnvironmentalHazard_Lava.asset";
        const string SpikeTrapPath = "Assets/Data/Traps/TrapDefinition_Spike_Visible.asset";
        const string LeverPath = "Assets/Data/Interactables/LeverSwitch_First.asset";

        [MenuItem("JRogue/Dungeon/Create Floor 1 Zone Pack")]
        public static void CreateFloor1ZonePack()
        {
            EnsureFolder(ZoneRoot);
            EnsureFolder(LayoutRoot);
            EnsureFolder(PopulationRoot);

            TileBase dungeonFloor = LoadTile(FloorTilePath);
            TileBase dungeonWall = LoadTile(WallTilePath);
            TileBase sandFloor = LoadTile(SandFloorPath);
            TileBase sandWall = LoadTile(SandWallPath);
            TileBase snowFloor = LoadTile(SnowFloorPath);
            TileBase snowWall = LoadTile(SnowWallPath);
            DungeonLayoutStamp dungeonStamp = AssetDatabase.LoadAssetAtPath<DungeonLayoutStamp>(DungeonSubStampPath);
            EnemySpawnDefinition skeletonSpawn =
                AssetDatabase.LoadAssetAtPath<EnemySpawnDefinition>(SkeletonSpawnPath);
            ItemData handheldTorch = AssetDatabase.LoadAssetAtPath<ItemData>(HandheldTorchPath);
            EnvironmentalHazardDefinition lavaHazard =
                AssetDatabase.LoadAssetAtPath<EnvironmentalHazardDefinition>(LavaHazardPath);
            TrapDefinition spikeTrap = AssetDatabase.LoadAssetAtPath<TrapDefinition>(SpikeTrapPath);
            InteractableTileDefinition lever =
                AssetDatabase.LoadAssetAtPath<InteractableTileDefinition>(LeverPath);

            DungeonZonePopulationProfile dungeonPopulation = CreateOrUpdatePopulationProfile(
                PopulationRoot + "/Population_Dungeon_Floor01.asset",
                spawnDefinition: skeletonSpawn,
                enemyMin: 4,
                enemyMax: 6,
                itemData: handheldTorch,
                itemMin: 0,
                itemMax: 1,
                hazardDefinition: lavaHazard,
                hazardMin: 2,
                hazardMax: 4,
                trapDefinition: spikeTrap,
                trapMin: 1,
                trapMax: 3,
                interactableDefinition: lever,
                interactableMin: 1,
                interactableMax: 2);
            DungeonZonePopulationProfile desertPopulation = CreateOrUpdatePopulationProfile(
                PopulationRoot + "/Population_Desert_Floor01.asset",
                spawnDefinition: skeletonSpawn,
                enemyMin: 2,
                enemyMax: 4,
                itemData: null,
                itemMin: 0,
                itemMax: 0,
                hazardDefinition: null,
                hazardMin: 0,
                hazardMax: 0,
                trapDefinition: null,
                trapMin: 0,
                trapMax: 0,
                interactableDefinition: null,
                interactableMin: 0,
                interactableMax: 0,
                enemyDensityMode: ZonePopulationDensityMode.DensityPer100Tiles,
                enemyRequiresTag: "outdoor");
            DungeonZonePopulationProfile snowPopulation = CreateOrUpdatePopulationProfile(
                PopulationRoot + "/Population_Snow_Floor01.asset",
                spawnDefinition: skeletonSpawn,
                enemyMin: 1,
                enemyMax: 3,
                itemData: null,
                itemMin: 0,
                itemMax: 0,
                hazardDefinition: null,
                hazardMin: 0,
                hazardMax: 0,
                trapDefinition: null,
                trapMin: 0,
                trapMax: 0,
                interactableDefinition: null,
                interactableMin: 0,
                interactableMax: 0,
                enemyForbiddenNearEdge: 2);

            DungeonZoneDefinition zoneDungeon = CreateOrUpdateZone(
                ZoneRoot + "/Zone_Dungeon.asset",
                "dungeon",
                "Dungeon Hub",
                dungeonFloor,
                dungeonWall,
                new ZoneFillProfile
                {
                    mode = ZoneFillMode.SubStamp,
                    subStampTable = new[]
                    {
                        new ZoneSubStampEntry { stamp = dungeonStamp, weight = 1 },
                    },
                },
                dungeonPopulation);
            DungeonZoneDefinition zoneDesert = CreateOrUpdateZone(
                ZoneRoot + "/Zone_Desert.asset",
                "desert",
                "Desert",
                sandFloor,
                sandWall,
                new ZoneFillProfile
                {
                    mode = ZoneFillMode.OpenPocket,
                    innerWallDensity = 10,
                },
                desertPopulation,
                tags: new[] { "outdoor" });
            DungeonZoneDefinition zoneSnow = CreateOrUpdateZone(
                ZoneRoot + "/Zone_Snow.asset",
                "snow",
                "Snow",
                snowFloor,
                snowWall,
                new ZoneFillProfile
                {
                    mode = ZoneFillMode.OpenPocket,
                    innerWallDensity = 10,
                },
                snowPopulation);

            DungeonFloorZoneLayout layout = CreateOrUpdateFloor01Layout(
                zoneDungeon,
                zoneDesert,
                zoneSnow);

            ApplyFloor01Definition(layout);
            AssetDatabase.SaveAssets();
            Debug.Log("[Dungeon] Floor 1 zone pack created. Floor_dungeon_floor_01 set to ZoneComposite.");
        }

        [MenuItem("JRogue/Dungeon/Create Floor 3 Zone Pack (Barbarian Jigsaw)")]
        public static void CreateFloor3ZonePack()
        {
            EnsureFolder(ZoneRoot);
            EnsureFolder(LayoutRoot);
            EnsureFolder(PopulationRoot);

            TileBase dungeonFloor = LoadTile(FloorTilePath);
            TileBase dungeonWall = LoadTile(WallTilePath);
            TileBase snowFloor = LoadTile(SnowFloorPath);
            TileBase snowWall = LoadTile(SnowWallPath);
            TileBase sandFloor = LoadTile(SandFloorPath);
            TileBase sandWall = LoadTile(SandWallPath);
            EnemySpawnDefinition skeletonSpawn =
                AssetDatabase.LoadAssetAtPath<EnemySpawnDefinition>(SkeletonSpawnPath);
            TrapDefinition spikeTrap = AssetDatabase.LoadAssetAtPath<TrapDefinition>(SpikeTrapPath);

            DungeonZonePopulationProfile orcPopulation = CreateOrUpdatePopulationProfile(
                PopulationRoot + "/Population_OrcCastle_Floor03.asset",
                spawnDefinition: skeletonSpawn,
                enemyMin: 6,
                enemyMax: 10,
                itemData: null,
                itemMin: 0,
                itemMax: 0,
                hazardDefinition: null,
                hazardMin: 0,
                hazardMax: 0,
                trapDefinition: spikeTrap,
                trapMin: 2,
                trapMax: 4,
                interactableDefinition: null,
                interactableMin: 0,
                interactableMax: 0);
            DungeonZonePopulationProfile witchPopulation = CreateOrUpdatePopulationProfile(
                PopulationRoot + "/Population_WitchForest_Floor03.asset",
                spawnDefinition: skeletonSpawn,
                enemyMin: 2,
                enemyMax: 4,
                itemData: null,
                itemMin: 0,
                itemMax: 0,
                hazardDefinition: null,
                hazardMin: 0,
                hazardMax: 0,
                trapDefinition: null,
                trapMin: 0,
                trapMax: 0,
                interactableDefinition: null,
                interactableMin: 0,
                interactableMax: 0,
                enemyRequiresTag: "outdoor",
                enemyForbiddenNearEdge: 2);
            DungeonZonePopulationProfile mountainPopulation = CreateOrUpdatePopulationProfile(
                PopulationRoot + "/Population_Mountain_Floor03.asset",
                spawnDefinition: skeletonSpawn,
                enemyMin: 1,
                enemyMax: 3,
                itemData: null,
                itemMin: 0,
                itemMax: 0,
                hazardDefinition: null,
                hazardMin: 0,
                hazardMax: 0,
                trapDefinition: spikeTrap,
                trapMin: 1,
                trapMax: 2,
                interactableDefinition: null,
                interactableMin: 0,
                interactableMax: 0,
                enemyForbiddenNearEdge: 1);

            DungeonZoneDefinition zoneOrcCastle = CreateOrUpdateZone(
                ZoneRoot + "/Zone_OrcCastle.asset",
                "orc_castle",
                "Orc Castle",
                dungeonFloor,
                dungeonWall,
                new ZoneFillProfile
                {
                    mode = ZoneFillMode.RoomCorridor,
                    ensureConnectivity = true,
                },
                orcPopulation,
                minWidth: 12,
                minHeight: 10,
                maxWidth: 16,
                maxHeight: 14);
            DungeonZoneDefinition zoneWitchForest = CreateOrUpdateZone(
                ZoneRoot + "/Zone_WitchForest.asset",
                "witch_forest",
                "Witch Forest",
                snowFloor,
                snowWall,
                new ZoneFillProfile
                {
                    mode = ZoneFillMode.Cave,
                    innerWallDensity = 35,
                    ensureConnectivity = true,
                },
                witchPopulation,
                tags: new[] { "outdoor" },
                minWidth: 12,
                minHeight: 10,
                maxWidth: 16,
                maxHeight: 14);
            DungeonZoneDefinition zoneMountain = CreateOrUpdateZone(
                ZoneRoot + "/Zone_Mountain.asset",
                "mountain",
                "Mountain Pass",
                sandFloor,
                sandWall,
                new ZoneFillProfile
                {
                    mode = ZoneFillMode.OpenPocket,
                    innerWallDensity = 25,
                    ensureConnectivity = true,
                },
                mountainPopulation,
                minWidth: 10,
                minHeight: 10,
                maxWidth: 14,
                maxHeight: 14);

            DungeonFloorZoneLayout layout = CreateOrUpdateFloor03Layout(
                zoneOrcCastle,
                zoneWitchForest,
                zoneMountain);

            ApplyFloorDefinition(
                Floor03DefPath,
                "dungeon_floor_03",
                layout,
                createIfMissing: true);
            AssetDatabase.SaveAssets();
            Debug.Log(
                "[Dungeon] Floor 3 zone pack created. Assign Floor_dungeon_floor_03 to " +
                "DungeonFloorInstanceManager.floorDefinitions to playtest the jigsaw layout.");
        }

        static DungeonZoneDefinition CreateOrUpdateZone(
            string path,
            string zoneId,
            string displayName,
            TileBase floorTile,
            TileBase wallTile,
            ZoneFillProfile fillProfile,
            DungeonZonePopulationProfile populationProfile = null,
            string[] tags = null,
            int minWidth = 8,
            int minHeight = 8,
            int maxWidth = 24,
            int maxHeight = 24)
        {
            var zone = AssetDatabase.LoadAssetAtPath<DungeonZoneDefinition>(path);
            if (zone == null)
            {
                zone = ScriptableObject.CreateInstance<DungeonZoneDefinition>();
                AssetDatabase.CreateAsset(zone, path);
            }

            SerializedObject so = new SerializedObject(zone);
            so.FindProperty("zoneId").stringValue = zoneId;
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("floorTile").objectReferenceValue = floorTile;
            so.FindProperty("wallTile").objectReferenceValue = wallTile;
            so.FindProperty("minWidth").intValue = minWidth;
            so.FindProperty("minHeight").intValue = minHeight;
            so.FindProperty("maxWidth").intValue = maxWidth;
            so.FindProperty("maxHeight").intValue = maxHeight;
            SerializedProperty fill = so.FindProperty("fillProfile");
            fill.FindPropertyRelative("mode").enumValueIndex = (int)fillProfile.mode;
            fill.FindPropertyRelative("innerWallDensity").intValue = fillProfile.innerWallDensity;
            fill.FindPropertyRelative("ensureConnectivity").boolValue = fillProfile.ensureConnectivity;
            SerializedProperty subStamps = fill.FindPropertyRelative("subStampTable");
            ZoneSubStampEntry[] table = fillProfile.subStampTable;
            if (table == null || table.Length == 0)
            {
                subStamps.arraySize = 0;
            }
            else
            {
                subStamps.arraySize = table.Length;
                for (int i = 0; i < table.Length; i++)
                {
                    SerializedProperty entry = subStamps.GetArrayElementAtIndex(i);
                    entry.FindPropertyRelative("stamp").objectReferenceValue = table[i].stamp;
                    entry.FindPropertyRelative("weight").intValue = table[i].weight;
                }
            }
            so.FindProperty("populationProfile").objectReferenceValue = populationProfile;
            SerializedProperty tagsProperty = so.FindProperty("tags");
            if (tags == null || tags.Length == 0)
            {
                tagsProperty.arraySize = 0;
            }
            else
            {
                tagsProperty.arraySize = tags.Length;
                for (int i = 0; i < tags.Length; i++)
                    tagsProperty.GetArrayElementAtIndex(i).stringValue = tags[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(zone);
            return zone;
        }

        static DungeonZonePopulationProfile CreateOrUpdatePopulationProfile(
            string path,
            EnemySpawnDefinition spawnDefinition,
            int enemyMin,
            int enemyMax,
            ItemData itemData,
            int itemMin,
            int itemMax,
            EnvironmentalHazardDefinition hazardDefinition,
            int hazardMin,
            int hazardMax,
            TrapDefinition trapDefinition,
            int trapMin,
            int trapMax,
            InteractableTileDefinition interactableDefinition,
            int interactableMin,
            int interactableMax,
            ZonePopulationDensityMode enemyDensityMode = ZonePopulationDensityMode.ScatterCount,
            string enemyRequiresTag = null,
            int enemyForbiddenNearEdge = 0)
        {
            var profile = AssetDatabase.LoadAssetAtPath<DungeonZonePopulationProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<DungeonZonePopulationProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }

            SerializedObject so = new SerializedObject(profile);
            SerializedProperty enemies = so.FindProperty("enemyPopulation");
            if (spawnDefinition != null)
            {
                enemies.arraySize = 1;
                SerializedProperty enemy = enemies.GetArrayElementAtIndex(0);
                enemy.FindPropertyRelative("spawnDefinition").objectReferenceValue = spawnDefinition;
                enemy.FindPropertyRelative("minCount").intValue = enemyMin;
                enemy.FindPropertyRelative("maxCount").intValue = enemyMax;
                enemy.FindPropertyRelative("weight").intValue = 0;
                enemy.FindPropertyRelative("densityMode").enumValueIndex = (int)enemyDensityMode;
                enemy.FindPropertyRelative("requiresTag").stringValue = enemyRequiresTag ?? string.Empty;
                enemy.FindPropertyRelative("forbiddenNearEdge").intValue = enemyForbiddenNearEdge;
            }
            else
            {
                enemies.arraySize = 0;
            }

            SerializedProperty items = so.FindProperty("floorItemPopulation");
            if (itemData != null)
            {
                items.arraySize = 1;
                SerializedProperty item = items.GetArrayElementAtIndex(0);
                item.FindPropertyRelative("itemData").objectReferenceValue = itemData;
                item.FindPropertyRelative("minCount").intValue = itemMin;
                item.FindPropertyRelative("maxCount").intValue = itemMax;
                item.FindPropertyRelative("minQuantity").intValue = 1;
                item.FindPropertyRelative("maxQuantity").intValue = 1;
            }
            else
            {
                items.arraySize = 0;
            }

            SerializedProperty hazards = so.FindProperty("hazardPopulation");
            if (hazardDefinition != null)
            {
                hazards.arraySize = 1;
                SerializedProperty hazard = hazards.GetArrayElementAtIndex(0);
                hazard.FindPropertyRelative("definition").objectReferenceValue = hazardDefinition;
                hazard.FindPropertyRelative("minCount").intValue = hazardMin;
                hazard.FindPropertyRelative("maxCount").intValue = hazardMax;
                hazard.FindPropertyRelative("startHidden").boolValue = false;
            }
            else
            {
                hazards.arraySize = 0;
            }

            SerializedProperty traps = so.FindProperty("trapPopulation");
            if (trapDefinition != null)
            {
                traps.arraySize = 1;
                SerializedProperty trap = traps.GetArrayElementAtIndex(0);
                trap.FindPropertyRelative("definition").objectReferenceValue = trapDefinition;
                trap.FindPropertyRelative("minCount").intValue = trapMin;
                trap.FindPropertyRelative("maxCount").intValue = trapMax;
            }
            else
            {
                traps.arraySize = 0;
            }

            SerializedProperty interactables = so.FindProperty("interactablePopulation");
            if (interactableDefinition != null)
            {
                interactables.arraySize = 1;
                SerializedProperty interactable = interactables.GetArrayElementAtIndex(0);
                interactable.FindPropertyRelative("definition").objectReferenceValue = interactableDefinition;
                interactable.FindPropertyRelative("minCount").intValue = interactableMin;
                interactable.FindPropertyRelative("maxCount").intValue = interactableMax;
            }
            else
            {
                interactables.arraySize = 0;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            return profile;
        }

        static DungeonFloorZoneLayout CreateOrUpdateFloor01Layout(
            DungeonZoneDefinition zoneDungeon,
            DungeonZoneDefinition zoneDesert,
            DungeonZoneDefinition zoneSnow)
        {
            var layout = AssetDatabase.LoadAssetAtPath<DungeonFloorZoneLayout>(Floor01LayoutPath);
            if (layout == null)
            {
                layout = ScriptableObject.CreateInstance<DungeonFloorZoneLayout>();
                AssetDatabase.CreateAsset(layout, Floor01LayoutPath);
            }

            SerializedObject so = new SerializedObject(layout);
            so.FindProperty("floorWidth").intValue = 30;
            so.FindProperty("floorHeight").intValue = 30;
            so.FindProperty("layoutKind").enumValueIndex = (int)ZoneLayoutKind.CompassSlots;
            so.FindProperty("fallbackZoneId").stringValue = ZoneIds.Rock;
            so.FindProperty("defaultOuterBoundary").enumValueIndex = (int)ZoneBoundaryKind.Wall;

            so.FindProperty("zoneDefinitions").arraySize = 3;
            so.FindProperty("zoneDefinitions").GetArrayElementAtIndex(0).objectReferenceValue = zoneDungeon;
            so.FindProperty("zoneDefinitions").GetArrayElementAtIndex(1).objectReferenceValue = zoneDesert;
            so.FindProperty("zoneDefinitions").GetArrayElementAtIndex(2).objectReferenceValue = zoneSnow;

            SerializedProperty rules = so.FindProperty("selectionRules");
            rules.arraySize = 2;
            SetSelectionRule(rules.GetArrayElementAtIndex(0), "desert", 60, excludes: new[] { "snow" });
            SetSelectionRule(rules.GetArrayElementAtIndex(1), "snow", 40, excludes: new[] { "desert" });

            SerializedProperty pieces = so.FindProperty("pieces");
            pieces.arraySize = 3;
            SetPiece(
                pieces.GetArrayElementAtIndex(0),
                "center",
                CompassDirection.Center,
                mandatory: true,
                isPlayerStart: true,
                candidates: new[] { ("dungeon", 1) });
            SetPiece(
                pieces.GetArrayElementAtIndex(1),
                "north",
                CompassDirection.North,
                mandatory: false,
                isPlayerStart: false,
                candidates: new[] { ("snow", 40), (ZoneIds.Empty, 60) });
            SetPiece(
                pieces.GetArrayElementAtIndex(2),
                "east",
                CompassDirection.East,
                mandatory: false,
                isPlayerStart: false,
                candidates: new[] { ("desert", 55), (ZoneIds.Empty, 45) });

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(layout);
            return layout;
        }

        static DungeonFloorZoneLayout CreateOrUpdateFloor03Layout(
            DungeonZoneDefinition zoneOrcCastle,
            DungeonZoneDefinition zoneWitchForest,
            DungeonZoneDefinition zoneMountain)
        {
            var layout = AssetDatabase.LoadAssetAtPath<DungeonFloorZoneLayout>(Floor03LayoutPath);
            if (layout == null)
            {
                layout = ScriptableObject.CreateInstance<DungeonFloorZoneLayout>();
                AssetDatabase.CreateAsset(layout, Floor03LayoutPath);
            }

            SerializedObject so = new SerializedObject(layout);
            so.FindProperty("floorWidth").intValue = 40;
            so.FindProperty("floorHeight").intValue = 30;
            so.FindProperty("layoutKind").enumValueIndex = (int)ZoneLayoutKind.ExplicitPieces;
            so.FindProperty("fallbackZoneId").stringValue = ZoneIds.Rock;
            so.FindProperty("defaultOuterBoundary").enumValueIndex = (int)ZoneBoundaryKind.Wall;

            so.FindProperty("zoneDefinitions").arraySize = 3;
            so.FindProperty("zoneDefinitions").GetArrayElementAtIndex(0).objectReferenceValue = zoneOrcCastle;
            so.FindProperty("zoneDefinitions").GetArrayElementAtIndex(1).objectReferenceValue = zoneWitchForest;
            so.FindProperty("zoneDefinitions").GetArrayElementAtIndex(2).objectReferenceValue = zoneMountain;

            SerializedProperty rules = so.FindProperty("selectionRules");
            rules.arraySize = 1;
            SetSelectionRule(
                rules.GetArrayElementAtIndex(0),
                "witch_forest",
                weight: 1,
                requiresAll: new[] { "orc_castle" });

            SerializedProperty pieces = so.FindProperty("pieces");
            pieces.arraySize = 3;
            SetExplicitPiece(
                pieces.GetArrayElementAtIndex(0),
                "west",
                mandatory: true,
                isPlayerStart: true,
                connectsTo: new[] { "center" },
                candidates: new[] { ("orc_castle", 1) },
                defaultBoundary: ZoneBoundaryKind.Open);
            SetExplicitPiece(
                pieces.GetArrayElementAtIndex(1),
                "center",
                mandatory: true,
                isPlayerStart: false,
                connectsTo: new[] { "west", "east" },
                candidates: new[] { ("witch_forest", 1) },
                defaultBoundary: ZoneBoundaryKind.Open);
            SetExplicitPiece(
                pieces.GetArrayElementAtIndex(2),
                "east",
                mandatory: true,
                isPlayerStart: false,
                connectsTo: new[] { "center" },
                candidates: new[] { ("mountain", 1) },
                defaultBoundary: ZoneBoundaryKind.Wall);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(layout);
            return layout;
        }

        static void SetSelectionRule(
            SerializedProperty element,
            string zoneId,
            int weight,
            string[] excludes = null,
            string[] requiresAll = null)
        {
            element.FindPropertyRelative("zoneId").stringValue = zoneId;
            element.FindPropertyRelative("weight").intValue = weight;
            element.FindPropertyRelative("mandatory").boolValue = false;
            element.FindPropertyRelative("maxInstances").intValue = 1;
            SetStringArray(element.FindPropertyRelative("excludes"), excludes);
            SetStringArray(element.FindPropertyRelative("requiresAll"), requiresAll);
        }

        static void SetPiece(
            SerializedProperty element,
            string pieceId,
            CompassDirection compass,
            bool mandatory,
            bool isPlayerStart,
            (string zoneId, int weight)[] candidates)
        {
            element.FindPropertyRelative("pieceId").stringValue = pieceId;
            element.FindPropertyRelative("anchorKind").enumValueIndex = (int)ZonePieceAnchorKind.Compass;
            element.FindPropertyRelative("compassDirection").enumValueIndex = (int)compass;
            element.FindPropertyRelative("mandatory").boolValue = mandatory;
            element.FindPropertyRelative("isPlayerStartPiece").boolValue = isPlayerStart;
            element.FindPropertyRelative("defaultBoundary").enumValueIndex = (int)ZoneBoundaryKind.Open;

            SerializedProperty candidateArray = element.FindPropertyRelative("candidates");
            candidateArray.arraySize = candidates.Length;
            for (int i = 0; i < candidates.Length; i++)
            {
                SerializedProperty candidate = candidateArray.GetArrayElementAtIndex(i);
                candidate.FindPropertyRelative("zoneId").stringValue = candidates[i].zoneId;
                candidate.FindPropertyRelative("weight").intValue = candidates[i].weight;
            }
        }

        static void SetExplicitPiece(
            SerializedProperty element,
            string pieceId,
            bool mandatory,
            bool isPlayerStart,
            string[] connectsTo,
            (string zoneId, int weight)[] candidates,
            ZoneBoundaryKind defaultBoundary)
        {
            element.FindPropertyRelative("pieceId").stringValue = pieceId;
            element.FindPropertyRelative("anchorKind").enumValueIndex = (int)ZonePieceAnchorKind.NormalizedRect;
            element.FindPropertyRelative("mandatory").boolValue = mandatory;
            element.FindPropertyRelative("isPlayerStartPiece").boolValue = isPlayerStart;
            element.FindPropertyRelative("defaultBoundary").enumValueIndex = (int)defaultBoundary;
            SetStringArray(element.FindPropertyRelative("connectsTo"), connectsTo);

            SerializedProperty normalized = element.FindPropertyRelative("normalizedRect");
            normalized.FindPropertyRelative("xMin").floatValue = 0f;
            normalized.FindPropertyRelative("yMin").floatValue = 0f;
            normalized.FindPropertyRelative("xMax").floatValue = 0f;
            normalized.FindPropertyRelative("yMax").floatValue = 0f;

            SerializedProperty candidateArray = element.FindPropertyRelative("candidates");
            candidateArray.arraySize = candidates.Length;
            for (int i = 0; i < candidates.Length; i++)
            {
                SerializedProperty candidate = candidateArray.GetArrayElementAtIndex(i);
                candidate.FindPropertyRelative("zoneId").stringValue = candidates[i].zoneId;
                candidate.FindPropertyRelative("weight").intValue = candidates[i].weight;
            }
        }

        static void SetStringArray(SerializedProperty property, string[] values)
        {
            if (values == null || values.Length == 0)
            {
                property.arraySize = 0;
                return;
            }

            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).stringValue = values[i];
        }

        static void ApplyFloor01Definition(DungeonFloorZoneLayout layout)
        {
            ApplyFloorDefinition(Floor01DefPath, "dungeon_floor_01", layout, createIfMissing: false);
        }

        static void ApplyFloorDefinition(
            string path,
            string floorId,
            DungeonFloorZoneLayout layout,
            bool createIfMissing)
        {
            var floorDef = AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>(path);
            if (floorDef == null)
            {
                if (!createIfMissing)
                {
                    Debug.LogError($"[Dungeon] Missing {path}");
                    return;
                }

                floorDef = ScriptableObject.CreateInstance<DungeonFloorDefinition>();
                AssetDatabase.CreateAsset(floorDef, path);
                CopyFloorDefinitionBaseline(Floor01DefPath, floorDef);
            }

            SerializedObject so = new SerializedObject(floorDef);
            so.FindProperty("floorId").stringValue = floorId;
            so.FindProperty("layoutMode").enumValueIndex = (int)FloorLayoutMode.ZoneComposite;
            so.FindProperty("zoneLayout").objectReferenceValue = layout;
            so.FindProperty("useFloorPopulationAsFallback").boolValue = true;
            so.FindProperty("playerSafeRadius").intValue = 5;
            so.FindProperty("participatesInDungeonTime").boolValue = true;
            if (floorId == "dungeon_floor_03")
            {
                so.FindProperty("additionalDayNightCycles").intValue = 4;
                SetTaggedRegionPortalRule(
                    so.FindProperty("portalPlacementRules"),
                    zoneId: "witch_forest",
                    metric: TaggedRegionPortalMetric.MaxY,
                    portalLinkId: "link_floor03_to_floor04",
                    targetFloorId: "dungeon_floor_04",
                    listLabel: "Portal (Forest Depth)",
                    minChebyshevFromStart: 3);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(floorDef);
        }

        static void SetTaggedRegionPortalRule(
            SerializedProperty rules,
            string zoneId,
            TaggedRegionPortalMetric metric,
            string portalLinkId,
            string targetFloorId,
            string listLabel,
            int minChebyshevFromStart)
        {
            rules.arraySize = 1;
            SerializedProperty rule = rules.GetArrayElementAtIndex(0);
            rule.FindPropertyRelative("kind").enumValueIndex = (int)PortalPlacementRuleKind.TaggedRegionEdge;
            rule.FindPropertyRelative("portalLinkId").stringValue = portalLinkId;
            rule.FindPropertyRelative("targetFloorId").stringValue = targetFloorId;
            rule.FindPropertyRelative("listLabel").stringValue = listLabel;
            rule.FindPropertyRelative("zoneId").stringValue = zoneId;
            rule.FindPropertyRelative("metric").enumValueIndex = (int)metric;
            rule.FindPropertyRelative("minChebyshevFromStart").intValue = minChebyshevFromStart;
        }

        static void CopyFloorDefinitionBaseline(string templatePath, DungeonFloorDefinition target)
        {
            var template = AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>(templatePath);
            if (template == null)
                return;

            SerializedObject from = new SerializedObject(template);
            SerializedObject to = new SerializedObject(target);
            CopyObjectReference(from, to, "layoutStamp");
            CopyObjectReference(from, to, "floorTile");
            CopyObjectReference(from, to, "wallTile");
            CopyObjectReference(from, to, "formationProfile");
            CopyObjectReference(from, to, "vaultCatalog");
            to.FindProperty("baseDayNightCycles").intValue =
                from.FindProperty("baseDayNightCycles").intValue;
            to.FindProperty("playerTurnsPerDay").intValue =
                from.FindProperty("playerTurnsPerDay").intValue;
            to.FindProperty("playerTurnsPerNight").intValue =
                from.FindProperty("playerTurnsPerNight").intValue;
            to.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        static void CopyObjectReference(SerializedObject from, SerializedObject to, string propertyName)
        {
            to.FindProperty(propertyName).objectReferenceValue =
                from.FindProperty(propertyName).objectReferenceValue;
        }

        static TileBase LoadTile(string path) =>
            AssetDatabase.LoadAssetAtPath<TileBase>(path);

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folderName = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
#endif
