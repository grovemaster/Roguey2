#if UNITY_EDITOR
using System.IO;
using JRogue.Ability;
using JRogue.Item;
using JRogue.Item.World;
using JRogue.Manager.Inventory;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JRogue.Editor.Inventory
{
    public static class EvocableItemPackEditor
    {
        const string FireballAbilityPath = "Assets/Resources/Item/Ability/Fireball_Standard.asset";
        const string SuddenStrengthAbilityPath = "Assets/Resources/Item/Ability/SuddenStrength_Standard.asset";
        const string FanFireballPath = "Assets/Resources/Item/Evocable/Fan_of_Fireball.asset";
        const string FanMightPath = "Assets/Resources/Item/Evocable/Fan_of_Might.asset";
        const string WorldFireballPrefabPath = "Assets/Prefabs/Item/WorldItem_Fan_of_Fireball.prefab";
        const string WorldMightPrefabPath = "Assets/Prefabs/Item/WorldItem_Fan_of_Might.prefab";
        const string ScrollWorldPrefabPath = "Assets/Prefabs/Item/WorldItem_Scroll_Fireball.prefab";
        const string ScenePath = "Assets/Scenes/SampleScene.unity";
        const string BarbarianObjectName = "Party_Barbarian_Warrior";

        [MenuItem("JRogue/Inventory/Create Evocable v0 Assets")]
        public static void CreateV0Assets()
        {
            AbilityAction fireball = AssetDatabase.LoadAssetAtPath<AbilityAction>(FireballAbilityPath);
            AbilityAction suddenStrength = AssetDatabase.LoadAssetAtPath<AbilityAction>(SuddenStrengthAbilityPath);
            if (fireball == null || suddenStrength == null)
            {
                Debug.LogError("[Evocable] Missing Fireball_Standard or SuddenStrength_Standard ability assets.");
                return;
            }

            Directory.CreateDirectory("Assets/Resources/Item/Evocable");
            Directory.CreateDirectory("Assets/Prefabs/Item");

            EvocableItemData fanFireball = CreateOrLoadEvocable(
                FanFireballPath,
                "Fan of Fireball",
                maxCharges: 2,
                startingCharges: 2,
                consumesWhenEmpty: true,
                rechargeInterval: EvocableItemData.DefaultRechargeIntervalPlayerPhases,
                fireball,
                "Evocable:Fireball");

            EvocableItemData fanMight = CreateOrLoadEvocable(
                FanMightPath,
                "Fan of Might",
                maxCharges: 4,
                startingCharges: 4,
                consumesWhenEmpty: false,
                rechargeInterval: EvocableItemData.DefaultRechargeIntervalPlayerPhases,
                suddenStrength,
                "Evocable:Might");

            GameObject scrollPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScrollWorldPrefabPath);
            if (scrollPrefab != null)
            {
                CreateWorldPrefab(WorldFireballPrefabPath, fanFireball, scrollPrefab);
                CreateWorldPrefab(WorldMightPrefabPath, fanMight, scrollPrefab);
            }
            else
                Debug.LogWarning($"[Evocable] No template prefab at {ScrollWorldPrefabPath}; world prefabs skipped.");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Evocable] Created Fan_of_Fireball, Fan_of_Might, and world prefabs.");
        }

        static EvocableItemData CreateOrLoadEvocable(
            string path,
            string displayName,
            int maxCharges,
            int startingCharges,
            bool consumesWhenEmpty,
            int rechargeInterval,
            AbilityAction ability,
            string logTag)
        {
            var existing = AssetDatabase.LoadAssetAtPath<EvocableItemData>(path);
            if (existing != null)
            {
                existing.itemName = displayName;
                existing.maxCharges = maxCharges;
                existing.startingCharges = startingCharges;
                existing.consumesWhenEmpty = consumesWhenEmpty;
                existing.rechargeIntervalPlayerPhases = rechargeInterval;
                existing.invokeAbility = ability;
                existing.inventoryTargetedUseLogTag = logTag;
                existing.weight = 0.5f;
                existing.requiresAppraisal = false;
                existing.goldValue = consumesWhenEmpty ? 80 : 120;
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var asset = ScriptableObject.CreateInstance<EvocableItemData>();
            asset.itemName = displayName;
            asset.maxCharges = maxCharges;
            asset.startingCharges = startingCharges;
            asset.consumesWhenEmpty = consumesWhenEmpty;
            asset.rechargeIntervalPlayerPhases = rechargeInterval;
            asset.invokeAbility = ability;
            asset.inventoryTargetedUseLogTag = logTag;
            asset.weight = 0.5f;
            asset.requiresAppraisal = false;
            asset.goldValue = consumesWhenEmpty ? 80 : 120;
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static void CreateWorldPrefab(string path, EvocableItemData data, GameObject template)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var worldItem = existing.GetComponent<WorldItem>();
                if (worldItem != null)
                {
                    worldItem.data = data;
                    EditorUtility.SetDirty(existing);
                }

                return;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(template);
            instance.name = Path.GetFileNameWithoutExtension(path);
            var wi = instance.GetComponent<WorldItem>();
            if (wi != null)
                wi.data = data;

            PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
        }

        /// <summary>
        /// Adds Fan of Fireball (2/2 + 1/2) and Fan of Might (4/4 + 0/4) to Party_Barbarian_Warrior in SampleScene.
        /// Creates item assets first if they are missing.
        /// </summary>
        [MenuItem("JRogue/Inventory/Seed Evocables on Party Barbarian Warrior", false, 200)]
        public static void SeedEvocablesOnPartyBarbarianWarrior()
        {
            EvocableItemData fanFireball = AssetDatabase.LoadAssetAtPath<EvocableItemData>(FanFireballPath);
            EvocableItemData fanMight = AssetDatabase.LoadAssetAtPath<EvocableItemData>(FanMightPath);
            if (fanFireball == null || fanMight == null)
            {
                Debug.Log("[Evocable] Item assets missing — creating v0 assets, then seeding.");
                CreateV0Assets();
                fanFireball = AssetDatabase.LoadAssetAtPath<EvocableItemData>(FanFireballPath);
                fanMight = AssetDatabase.LoadAssetAtPath<EvocableItemData>(FanMightPath);
                if (fanFireball == null || fanMight == null)
                {
                    Debug.LogError("[Evocable] Could not create or load Fan_of_Fireball / Fan_of_Might.");
                    return;
                }
            }

            Scene scene = EditorSceneManager.GetActiveScene();
            GameObject barbarian = FindInScene(scene, BarbarianObjectName);
            if (barbarian == null)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                barbarian = FindInScene(scene, BarbarianObjectName);
            }

            if (barbarian == null)
            {
                Debug.LogError(
                    $"[Evocable] Could not find {BarbarianObjectName} in the active scene or {ScenePath}.");
                return;
            }

            InventoryManager inv = barbarian.GetComponentInChildren<InventoryManager>(true);
            if (inv == null)
            {
                Debug.LogError($"[Evocable] {BarbarianObjectName} has no {nameof(InventoryManager)}.");
                return;
            }

            SerializedObject so = new SerializedObject(inv);
            SerializedProperty carried = so.FindProperty("carriedItems");
            if (carried == null || !carried.isArray)
            {
                Debug.LogError("[Evocable] InventoryManager.carriedItems not found for serialization.");
                return;
            }

            RemoveEvocablesFromCarried(carried, fanFireball);
            RemoveEvocablesFromCarried(carried, fanMight);

            AppendEvocable(carried, fanFireball, currentCharges: 2);
            AppendEvocable(carried, fanFireball, currentCharges: 1);
            AppendEvocable(carried, fanMight, currentCharges: 4);
            AppendEvocable(carried, fanMight, currentCharges: 0);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(inv);
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = barbarian;
            Debug.Log(
                $"[Evocable] Seeded {BarbarianObjectName} in '{scene.path}': " +
                "Fan of Fireball 2/2 + 1/2, Fan of Might 4/4 + 0/4. Save the scene (Ctrl+S).");
        }

        [MenuItem("JRogue/Inventory/Seed Evocables on Party Barbarian Warrior", true)]
        public static bool SeedEvocablesOnPartyBarbarianWarriorValidate() => !Application.isPlaying;

        static void AppendEvocable(SerializedProperty carried, EvocableItemData definition, int currentCharges)
        {
            int index = carried.arraySize;
            carried.InsertArrayElementAtIndex(index);
            SerializedProperty entry = carried.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("id").stringValue = System.Guid.NewGuid().ToString("N");
            entry.FindPropertyRelative("definition").objectReferenceValue = definition;
            entry.FindPropertyRelative("quantity").intValue = 1;
            entry.FindPropertyRelative("storageLocation").enumValueIndex = (int)ItemStorageLocation.Carried;
            entry.FindPropertyRelative("isAppraised").boolValue = true;
            entry.FindPropertyRelative("currentCharges").intValue =
                Mathf.Clamp(currentCharges, 0, definition.maxCharges);
            entry.FindPropertyRelative("maxCharges").intValue = definition.maxCharges;
            entry.FindPropertyRelative("rechargePhasesAccumulated").intValue = 0;
        }

        static void RemoveEvocablesFromCarried(SerializedProperty carried, EvocableItemData definition)
        {
            for (int i = carried.arraySize - 1; i >= 0; i--)
            {
                SerializedProperty def = carried.GetArrayElementAtIndex(i).FindPropertyRelative("definition");
                if (def.objectReferenceValue == definition)
                    carried.DeleteArrayElementAtIndex(i);
            }
        }

        [MenuItem("JRogue/Inventory/Place Evocable Pickups in SampleScene")]
        public static void PlacePickupsInSampleScene()
        {
            GameObject fireballPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WorldFireballPrefabPath);
            GameObject mightPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WorldMightPrefabPath);
            if (fireballPrefab == null || mightPrefab == null)
            {
                Debug.LogError("[Evocable] Create Evocable v0 Assets first.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            PlaceIfMissing(scene, fireballPrefab, "WorldItem_Fan_of_Fireball (Sample)", new Vector3(4f, -0.5f, 0f));
            PlaceIfMissing(scene, mightPrefab, "WorldItem_Fan_of_Might (Sample)", new Vector3(5f, -0.5f, 0f));
            EditorSceneManager.MarkSceneDirty(scene);
        }

        static void PlaceIfMissing(Scene scene, GameObject prefab, string objectName, Vector3 position)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == objectName)
                {
                    Debug.Log($"[Evocable] {objectName} already in scene.");
                    Selection.activeGameObject = root;
                    return;
                }
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = objectName;
            instance.transform.position = position;
            Selection.activeGameObject = instance;
            Debug.Log($"[Evocable] Placed {objectName} at {position}.");
        }

        static GameObject FindInScene(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform[] all = root.GetComponentsInChildren<Transform>(true);
                foreach (Transform t in all)
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
