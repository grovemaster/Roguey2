#if UNITY_EDITOR
using JRogue.Item;
using JRogue.Item.World;
using JRogue.Manager.Inventory;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JRogue.Editor.Inventory
{
    /// <summary>SampleScene helpers for <c>Potion_HealingPotion</c> pickup and barbarian seed.</summary>
    public static class HealingPotionSampleSceneSetup
    {
        const string PotionPrefabPath = "Assets/Prefabs/Item/WorldItem_Potion.prefab";
        const string ItemDataPath = "Assets/Resources/Item/Potion/Potion_HealingPotion.asset";
        const string ScenePath = "Assets/Scenes/SampleScene.unity";
        const string PlacedObjectName = "WorldItem_HealingPotion (Sample)";
        const string BarbarianObjectName = "Party_Barbarian_Warrior";
        const int SeedQuantity = 3;

        [MenuItem("JRogue/Inventory/Place Healing Potion in SampleScene")]
        public static void PlaceInSampleScene()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PotionPrefabPath);
            ItemData potionDef = AssetDatabase.LoadAssetAtPath<ItemData>(ItemDataPath);
            if (prefab == null)
            {
                Debug.LogError($"Missing prefab at {PotionPrefabPath}");
                return;
            }

            if (potionDef == null)
            {
                Debug.LogError($"Missing ItemData at {ItemDataPath}");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == PlacedObjectName)
                {
                    Debug.Log($"[Potion:Healing] {PlacedObjectName} already in scene.");
                    Selection.activeGameObject = root;
                    return;
                }
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = PlacedObjectName;
            instance.transform.position = new Vector3(4f, -0.5f, 0f);

            WorldItem worldItem = instance.GetComponent<WorldItem>();
            if (worldItem != null)
            {
                Undo.RecordObject(worldItem, "Place Healing Potion");
                worldItem.data = potionDef;
                EditorUtility.SetDirty(worldItem);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = instance;
            Debug.Log("[Potion:Healing] Placed Healing Potion pickup at (4, -0.5) in SampleScene.");
        }

        [MenuItem("JRogue/Inventory/Seed Healing Potions on Party_Barbarian_Warrior")]
        public static void SeedPotionsOnBarbarian()
        {
            ItemData potionDef = AssetDatabase.LoadAssetAtPath<ItemData>(ItemDataPath);
            if (potionDef == null)
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

            Undo.RecordObject(inv, "Seed Healing Potions");
            inv.EditorSeedCarriedItem(potionDef, SeedQuantity);
            EditorUtility.SetDirty(inv);

            GameObject prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(inv.gameObject);
            if (prefabRoot != null)
                PrefabUtility.RecordPrefabInstancePropertyModifications(inv);

            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = inv.gameObject;
            Debug.Log($"[Potion:Healing] Added Healing Potion ×{SeedQuantity} to {BarbarianObjectName} carried inventory.");
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
