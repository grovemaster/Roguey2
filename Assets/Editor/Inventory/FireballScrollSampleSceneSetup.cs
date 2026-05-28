#if UNITY_EDITOR
using JRogue.Item;
using JRogue.Manager.Inventory;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JRogue.Editor.Inventory
{
    /// <summary>Places <c>WorldItem_Scroll_Fireball</c> in SampleScene for AC1 pickup testing.</summary>
    public static class FireballScrollSampleSceneSetup
    {
        const string PrefabPath = "Assets/Prefabs/Item/WorldItem_Scroll_Fireball.prefab";
        const string ScenePath = "Assets/Scenes/SampleScene.unity";
        const string PlacedObjectName = "WorldItem_Scroll_Fireball (Sample)";

        [MenuItem("JRogue/Inventory/Place Fireball Scroll in SampleScene")]
        public static void PlaceInSampleScene()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"Missing prefab at {PrefabPath}");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == PlacedObjectName)
                {
                    Debug.Log($"[Scroll:Fireball] {PlacedObjectName} already in scene.");
                    Selection.activeGameObject = root;
                    return;
                }
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = PlacedObjectName;
            instance.transform.position = new Vector3(2f, -0.5f, 0f);
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = instance;
            Debug.Log("[Scroll:Fireball] Placed scroll pickup at (2, -0.5) in SampleScene.");
        }

        const string ScrollItemDataPath = "Assets/Resources/Item/Scroll/Scroll_Fireball.asset";
        const string BarbarianObjectName = "Party_Barbarian_Warrior";

        [MenuItem("JRogue/Inventory/Seed Fireball Scroll on Party_Barbarian_Warrior")]
        public static void SeedScrollOnBarbarian()
        {
            ItemData scrollDef = AssetDatabase.LoadAssetAtPath<ItemData>(ScrollItemDataPath);
            if (scrollDef == null)
            {
                Debug.LogError($"Missing ItemData at {ScrollItemDataPath}");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject barbarian = FindSceneObjectByName(scene, BarbarianObjectName);
            if (barbarian == null)
            {
                Debug.LogError($"Could not find {BarbarianObjectName} in {ScenePath}.");
                return;
            }

            InventoryManager inv = barbarian.GetComponentInChildren<InventoryManager>(true);
            if (inv == null)
            {
                Debug.LogError($"{BarbarianObjectName} has no {nameof(InventoryManager)}.");
                return;
            }

            SerializedObject so = new SerializedObject(inv);
            SerializedProperty carried = so.FindProperty("carriedItems");
            for (int i = 0; i < carried.arraySize; i++)
            {
                SerializedProperty def = carried.GetArrayElementAtIndex(i).FindPropertyRelative("definition");
                if (def.objectReferenceValue == scrollDef)
                {
                    Debug.Log($"[Scroll:Fireball] {BarbarianObjectName} already carries Scroll of Fireball.");
                    Selection.activeGameObject = barbarian;
                    return;
                }
            }

            int index = carried.arraySize;
            carried.InsertArrayElementAtIndex(index);
            SerializedProperty entry = carried.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("id").stringValue = System.Guid.NewGuid().ToString("N");
            entry.FindPropertyRelative("definition").objectReferenceValue = scrollDef;
            entry.FindPropertyRelative("quantity").intValue = 1;
            entry.FindPropertyRelative("storageLocation").enumValueIndex = (int)ItemStorageLocation.Carried;
            entry.FindPropertyRelative("isAppraised").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = barbarian;
            Debug.Log($"[Scroll:Fireball] Added Scroll of Fireball to {BarbarianObjectName} starting inventory.");
        }

        static GameObject FindSceneObjectByName(Scene scene, string objectName)
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
