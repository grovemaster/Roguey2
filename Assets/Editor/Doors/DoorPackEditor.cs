#if UNITY_EDITOR
using System.IO;
using JRogue.Data.Door;
using JRogue.Interactables;
using JRogue.Item;
using JRogue.Manager.Door;
using JRogue.Manager.Inventory;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JRogue.Editor.Doors
{
    public static class DoorPackEditor
    {
        const string ScenePath = "Assets/Scenes/SampleScene.unity";
        const string BarbarianObjectName = "Party_Barbarian_Warrior";
        const string DataRoot = "Assets/Data/Doors";
        const string ItemRoot = "Assets/Resources/Item/Key";
        const string KeyAssetPath = "Assets/Resources/Item/Key/Key_Test_A.asset";
        const string DoorSpritesRoot = "Assets/Art/Doors/Sprites";
        const string KeyIconPath = "Assets/Art/Items/Sprites/Key_Test_A.png";

        // SampleScene floor bounds: x -5..2, y -4..3 (party ~ (-1,-2))
        static readonly Vector3Int CellHorizontal = new Vector3Int(1, -2, 0);
        static readonly Vector3Int CellVerticalOpen = new Vector3Int(0, 1, 0);
        static readonly Vector3Int CellLocked = new Vector3Int(2, -2, 0);

        [MenuItem("JRogue/Doors/Create Door v0 Assets")]
        public static void CreateV0Assets()
        {
            Directory.CreateDirectory(DataRoot);
            Directory.CreateDirectory(ItemRoot);
            Directory.CreateDirectory("Assets/Data/Interactables/Effects");

            DoorDefinition horizontal = CreateDoor(
                $"{DataRoot}/Door_Test_Horizontal.asset",
                "Door_Test_Horizontal",
                DoorOrientation.Horizontal,
                startsLocked: false,
                startsOpen: false);

            DoorDefinition vertical = CreateDoor(
                $"{DataRoot}/Door_Test_Vertical.asset",
                "Door_Test_Vertical",
                DoorOrientation.Vertical,
                startsLocked: false,
                startsOpen: true);

            DoorDefinition locked = CreateDoor(
                $"{DataRoot}/Door_Test_Locked.asset",
                "Door_Test_Locked",
                DoorOrientation.Horizontal,
                startsLocked: true,
                startsOpen: false);

            DoorKeyItemData key = CreateKey($"{ItemRoot}/Key_Test_A.asset", "Key_Test_A", locked.doorId);

            UnlockDoorEffect unlock = AssetDatabase.LoadAssetAtPath<UnlockDoorEffect>(
                $"{DataRoot}/../Interactables/Effects/UnlockDoor_TestLockedDoor.asset");
            if (unlock == null)
            {
                unlock = ScriptableObject.CreateInstance<UnlockDoorEffect>();
                unlock.doorId = locked.doorId;
                AssetDatabase.CreateAsset(unlock, "Assets/Data/Interactables/Effects/UnlockDoor_TestLockedDoor.asset");
            }

            DoorPlacementSet set = AssetDatabase.LoadAssetAtPath<DoorPlacementSet>($"{DataRoot}/SampleScene_Doors.asset");
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<DoorPlacementSet>();
                AssetDatabase.CreateAsset(set, $"{DataRoot}/SampleScene_Doors.asset");
            }

            set.placements = new[]
            {
                new DoorPlacement { definition = horizontal, cell = CellHorizontal },
                new DoorPlacement
                {
                    definition = vertical,
                    cell = CellVerticalOpen,
                    overrideOpenState = true,
                    initialState = DoorState.Open,
                },
                new DoorPlacement
                {
                    definition = locked,
                    cell = CellLocked,
                    overrideLocked = true,
                    startsLocked = true,
                },
            };

            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Door] Created door definitions, key, unlock effect, and SampleScene_Doors placement set.");
        }

        [MenuItem("JRogue/Doors/Wire Door Service in SampleScene")]
        public static void WireDoorServiceInSampleScene()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (Object.FindAnyObjectByType<DoorService>() == null)
            {
                var go = new GameObject("DoorService");
                go.AddComponent<DoorService>();
                go.AddComponent<DoorTileBootstrap>();
            }

            DoorTileBootstrap bootstrap = Object.FindAnyObjectByType<DoorTileBootstrap>();
            DoorPlacementSet set = AssetDatabase.LoadAssetAtPath<DoorPlacementSet>($"{DataRoot}/SampleScene_Doors.asset");
            if (bootstrap != null && set != null)
            {
                SerializedObject so = new SerializedObject(bootstrap);
                so.FindProperty("placementSet").objectReferenceValue = set;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[Door] Wired DoorService + DoorTileBootstrap in SampleScene.");
        }

        [MenuItem("JRogue/Doors/Seed Test Key on Party Barbarian Warrior", false, 201)]
        public static void SeedKeyOnBarbarian()
        {
            DoorKeyItemData key = LoadKeyAsset();
            if (key == null)
            {
                Debug.Log("[Door] Key_Test_A missing — creating v0 assets, then seeding.");
                CreateV0Assets();
                key = LoadKeyAsset();
                if (key == null)
                {
                    Debug.LogError("[Door] Could not create or load Key_Test_A.");
                    return;
                }
            }

            Scene scene = EditorSceneManager.GetActiveScene();
            GameObject barbarian = FindByName(scene, BarbarianObjectName);
            if (barbarian == null)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                barbarian = FindByName(scene, BarbarianObjectName);
            }

            if (barbarian == null)
            {
                Debug.LogError(
                    $"[Door] Could not find {BarbarianObjectName} in the active scene or {ScenePath}.");
                return;
            }

            InventoryManager inv = barbarian.GetComponentInChildren<InventoryManager>(true);
            if (inv == null)
            {
                Debug.LogError($"[Door] {BarbarianObjectName} has no {nameof(InventoryManager)}.");
                return;
            }

            SerializedObject so = new SerializedObject(inv);
            SerializedProperty carried = so.FindProperty("carriedItems");
            if (carried == null || !carried.isArray)
            {
                Debug.LogError("[Door] InventoryManager.carriedItems not found for serialization.");
                return;
            }

            Undo.RecordObject(inv, "Seed Test Door Key");
            RemoveKeysFromCarried(carried, key);
            AppendKey(carried, key);
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(inv);
            GameObject prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(inv.gameObject);
            if (prefabRoot != null)
                PrefabUtility.RecordPrefabInstancePropertyModifications(inv);

            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = barbarian;
            Debug.Log(
                $"[Door] Seeded {BarbarianObjectName} in '{scene.path}' with Key_Test_A. Save the scene (Ctrl+S).");
        }

        [MenuItem("JRogue/Doors/Seed Test Key on Party Barbarian Warrior", true)]
        public static bool SeedKeyOnBarbarianValidate() => !Application.isPlaying;

        static DoorKeyItemData LoadKeyAsset() =>
            AssetDatabase.LoadAssetAtPath<DoorKeyItemData>(KeyAssetPath);

        static void AppendKey(SerializedProperty carried, DoorKeyItemData definition)
        {
            int index = carried.arraySize;
            carried.InsertArrayElementAtIndex(index);
            SerializedProperty entry = carried.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("id").stringValue = System.Guid.NewGuid().ToString("N");
            entry.FindPropertyRelative("definition").objectReferenceValue = definition;
            entry.FindPropertyRelative("quantity").intValue = 1;
            entry.FindPropertyRelative("storageLocation").enumValueIndex = (int)ItemStorageLocation.Carried;
            entry.FindPropertyRelative("isAppraised").boolValue = true;
        }

        static void RemoveKeysFromCarried(SerializedProperty carried, DoorKeyItemData definition)
        {
            for (int i = carried.arraySize - 1; i >= 0; i--)
            {
                SerializedProperty def = carried.GetArrayElementAtIndex(i).FindPropertyRelative("definition");
                if (def.objectReferenceValue == definition)
                    carried.DeleteArrayElementAtIndex(i);
            }
        }

        static DoorDefinition CreateDoor(
            string path,
            string doorId,
            DoorOrientation orientation,
            bool startsLocked,
            bool startsOpen)
        {
            var def = AssetDatabase.LoadAssetAtPath<DoorDefinition>(path)
                ?? ScriptableObject.CreateInstance<DoorDefinition>();
            def.doorId = doorId;
            def.displayName = doorId.Replace('_', ' ');
            def.orientation = orientation;
            def.startsLocked = startsLocked;
            def.startsOpen = startsOpen;
            ApplyDoorSprites(def);
            if (AssetDatabase.LoadAssetAtPath<DoorDefinition>(path) == null)
                AssetDatabase.CreateAsset(def, path);
            else
                EditorUtility.SetDirty(def);
            return def;
        }

        static DoorKeyItemData CreateKey(string path, string itemName, string targetDoorId)
        {
            var key = AssetDatabase.LoadAssetAtPath<DoorKeyItemData>(path)
                ?? ScriptableObject.CreateInstance<DoorKeyItemData>();
            key.itemName = itemName;
            key.targetDoorId = targetDoorId;
            key.category = ItemCategory.Key;
            key.weight = 0.1f;
            key.goldValue = 0;
            key.requiresAppraisal = false;
            key.isThrowable = false;
            ConfigureTextureImporter(KeyIconPath);
            key.icon = AssetDatabase.LoadAssetAtPath<Sprite>(KeyIconPath);
            if (AssetDatabase.LoadAssetAtPath<DoorKeyItemData>(path) == null)
                AssetDatabase.CreateAsset(key, path);
            else
                EditorUtility.SetDirty(key);
            return key;
        }

        static void ApplyDoorSprites(DoorDefinition def)
        {
            def.closedHorizontal = LoadDoorSprite("Door_Closed_H.png");
            def.openHorizontal = LoadDoorSprite("Door_Open_H.png");
            def.brokenHorizontal = LoadDoorSprite("Door_Broken_H.png");
            def.closedVertical = LoadDoorSprite("Door_Closed_V.png");
            def.openVertical = LoadDoorSprite("Door_Open_V.png");
            def.brokenVertical = LoadDoorSprite("Door_Broken_V.png");
        }

        static Sprite LoadDoorSprite(string fileName)
        {
            string path = $"{DoorSpritesRoot}/{fileName}";
            if (!File.Exists(path))
            {
                Debug.LogError($"[Door] Missing sprite at {path}. See Assets/Art/Doors/ThirdParty/DungeonCrawl32/README.md");
                return null;
            }

            ConfigureTextureImporter(path);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static void ConfigureTextureImporter(string assetPath)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 32;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        static GameObject FindByName(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == objectName)
                        return t.gameObject;
                }
            }

            return null;
        }
    }
}
#endif
