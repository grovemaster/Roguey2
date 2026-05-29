#if UNITY_EDITOR
using JRogue.Item;
using JRogue.Manager.Inventory;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JRogue.Editor.Inventory
{
    /// <summary>SampleScene helpers for <c>Missile_ThrowingKnife</c> pickup and barbarian seed (×5).</summary>
    public static class ThrowingKnifeSampleSceneSetup
    {
        const string PrefabPath = "Assets/Prefabs/Item/WorldItem_ThrowingKnife.prefab";
        const string ItemDataPath = "Assets/Resources/Item/Missile/Missile_ThrowingKnife.asset";
        const string ScenePath = "Assets/Scenes/SampleScene.unity";
        const string PlacedObjectName = "WorldItem_ThrowingKnife (Sample)";
        const string BarbarianObjectName = "Party_Barbarian_Warrior";
        const int SeedQuantity = 5;

        [MenuItem("JRogue/Inventory/Place Throwing Knife in SampleScene")]
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
                    Debug.Log($"[Missile:ThrowingKnife] {PlacedObjectName} already in scene.");
                    Selection.activeGameObject = root;
                    return;
                }
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = PlacedObjectName;
            instance.transform.position = new Vector3(3f, -0.5f, 0f);
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = instance;
            Debug.Log("[Missile:ThrowingKnife] Placed knife pickup at (3, -0.5) in SampleScene.");
        }

        [MenuItem("JRogue/Inventory/Seed Throwing Knives on Party_Barbarian_Warrior")]
        public static void SeedKnivesOnBarbarian()
        {
            ItemData knifeDef = AssetDatabase.LoadAssetAtPath<ItemData>(ItemDataPath);
            if (knifeDef == null)
            {
                Debug.LogError($"Missing ItemData at {ItemDataPath}");
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
                if (def.objectReferenceValue == knifeDef)
                {
                    SerializedProperty qty = carried.GetArrayElementAtIndex(i).FindPropertyRelative("quantity");
                    if (qty.intValue != SeedQuantity)
                    {
                        qty.intValue = SeedQuantity;
                        so.ApplyModifiedPropertiesWithoutUndo();
                        EditorSceneManager.MarkSceneDirty(scene);
                        Debug.Log($"[Missile:ThrowingKnife] Updated stack to ×{SeedQuantity} on {BarbarianObjectName}.");
                    }
                    else
                    {
                        Debug.Log($"[Missile:ThrowingKnife] {BarbarianObjectName} already carries Throwing Knife ×{SeedQuantity}.");
                    }

                    Selection.activeGameObject = barbarian;
                    return;
                }
            }

            int index = carried.arraySize;
            carried.InsertArrayElementAtIndex(index);
            SerializedProperty entry = carried.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("id").stringValue = System.Guid.NewGuid().ToString("N");
            entry.FindPropertyRelative("definition").objectReferenceValue = knifeDef;
            entry.FindPropertyRelative("quantity").intValue = SeedQuantity;
            entry.FindPropertyRelative("storageLocation").enumValueIndex = (int)ItemStorageLocation.Carried;
            entry.FindPropertyRelative("isAppraised").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = barbarian;
            Debug.Log($"[Missile:ThrowingKnife] Added Throwing Knife ×{SeedQuantity} to {BarbarianObjectName}.");
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
