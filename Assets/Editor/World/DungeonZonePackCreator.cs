#if UNITY_EDITOR
using System.IO;
using JRogue.Item;
using JRogue.Spawn;
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
        const string Floor01LayoutPath = LayoutRoot + "/Layout_Floor01_Zones.asset";
        const string PopulationRoot = ZoneRoot + "/Population";
        const string DungeonSubStampPath = "Assets/Resources/Dungeon/Stamp_Floor02_20x20.asset";
        const string SkeletonSpawnPath = "Assets/Resources/Dungeon/Spawn_DungeonTestSkeleton.asset";
        const string HandheldTorchPath = "Assets/Resources/Item/Accessory/Accessory_HandheldTorch.asset";

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

            DungeonZonePopulationProfile dungeonPopulation = CreateOrUpdatePopulationProfile(
                PopulationRoot + "/Population_Dungeon_Floor01.asset",
                skeletonSpawn,
                enemyMin: 4,
                enemyMax: 6,
                handheldTorch,
                itemMin: 0,
                itemMax: 1);
            DungeonZonePopulationProfile desertPopulation = CreateOrUpdatePopulationProfile(
                PopulationRoot + "/Population_Desert_Floor01.asset",
                skeletonSpawn,
                enemyMin: 2,
                enemyMax: 4,
                itemData: null,
                itemMin: 0,
                itemMax: 0);
            DungeonZonePopulationProfile snowPopulation = CreateOrUpdatePopulationProfile(
                PopulationRoot + "/Population_Snow_Floor01.asset",
                skeletonSpawn,
                enemyMin: 1,
                enemyMax: 3,
                itemData: null,
                itemMin: 0,
                itemMax: 0);

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
                desertPopulation);
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

        static DungeonZoneDefinition CreateOrUpdateZone(
            string path,
            string zoneId,
            string displayName,
            TileBase floorTile,
            TileBase wallTile,
            ZoneFillProfile fillProfile,
            DungeonZonePopulationProfile populationProfile = null)
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
            so.FindProperty("minWidth").intValue = 8;
            so.FindProperty("minHeight").intValue = 8;
            so.FindProperty("maxWidth").intValue = 24;
            so.FindProperty("maxHeight").intValue = 24;
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
            int itemMax)
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

        static void SetSelectionRule(
            SerializedProperty element,
            string zoneId,
            int weight,
            string[] excludes = null)
        {
            element.FindPropertyRelative("zoneId").stringValue = zoneId;
            element.FindPropertyRelative("weight").intValue = weight;
            element.FindPropertyRelative("mandatory").boolValue = false;
            element.FindPropertyRelative("maxInstances").intValue = 1;
            SetStringArray(element.FindPropertyRelative("excludes"), excludes);
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
            var floorDef = AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>(Floor01DefPath);
            if (floorDef == null)
            {
                Debug.LogError($"[Dungeon] Missing {Floor01DefPath}");
                return;
            }

            SerializedObject so = new SerializedObject(floorDef);
            so.FindProperty("layoutMode").enumValueIndex = (int)FloorLayoutMode.ZoneComposite;
            so.FindProperty("zoneLayout").objectReferenceValue = layout;
            so.FindProperty("useFloorPopulationAsFallback").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(floorDef);
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
