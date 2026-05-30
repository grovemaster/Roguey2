#if UNITY_EDITOR
using JRogue.Combat;
using JRogue.Item;
using JRogue.Manager.Inventory;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JRogue.Editor.Inventory
{
    /// <summary>SampleScene helpers for bow + arrow kit on <c>Party_Barbarian_Warrior</c> and world pickups.</summary>
    public static class BowSampleSceneSetup
    {
        const string BowPath = "Assets/Resources/Item/Weapon/Weapon_ShortBow.asset";
        const string StonePath = "Assets/Resources/Item/Missile/Missile_StoneArrow.asset";
        const string SteelPath = "Assets/Resources/Item/Missile/Missile_SteelArrow.asset";
        const string BowPrefabPath = "Assets/Prefabs/Item/WorldItem_ShortBow.prefab";
        const string StonePrefabPath = "Assets/Prefabs/Item/WorldItem_StoneArrow.prefab";
        const string SteelPrefabPath = "Assets/Prefabs/Item/WorldItem_SteelArrow.prefab";
        const string ScenePath = "Assets/Scenes/SampleScene.unity";
        const string BarbarianObjectName = "Party_Barbarian_Warrior";
        const string PlacedRootName = "BowKit (Sample)";
        const int StoneQty = 20;
        const int SteelQty = 10;

        [MenuItem("JRogue/Inventory/Seed Bow Kit on Party_Barbarian_Warrior")]
        public static void SeedBowKitOnBarbarian()
        {
            ItemData bow = AssetDatabase.LoadAssetAtPath<ItemData>(BowPath);
            ItemData stone = AssetDatabase.LoadAssetAtPath<ItemData>(StonePath);
            ItemData steel = AssetDatabase.LoadAssetAtPath<ItemData>(SteelPath);
            if (bow == null || stone == null || steel == null)
            {
                Debug.LogError("[Bow] Missing bow/arrow ItemData assets. Reimport project.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject barbarian = FindSceneObjectByName(scene, BarbarianObjectName);
            if (barbarian == null)
            {
                Debug.LogError($"[Bow] Could not find {BarbarianObjectName} in {ScenePath}.");
                return;
            }

            InventoryManager inv = barbarian.GetComponentInChildren<InventoryManager>(true);
            if (inv == null)
            {
                Debug.LogError($"[Bow] {BarbarianObjectName} has no {nameof(InventoryManager)}.");
                return;
            }

            Undo.RecordObject(inv, "Seed Bow Kit");
            inv.EditorReplaceBowKitItems(bow, stone, steel, StoneQty, SteelQty);
            EditorUtility.SetDirty(inv);

            BowKitSampleSceneBootstrap bootstrap = barbarian.GetComponent<BowKitSampleSceneBootstrap>();
            if (bootstrap == null)
                bootstrap = Undo.AddComponent<BowKitSampleSceneBootstrap>(barbarian);

            Undo.RecordObject(bootstrap, "Seed Bow Kit");
            bootstrap.EditorConfigure(bow, stone, steel);
            EditorUtility.SetDirty(bootstrap);

            GameObject prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(inv.gameObject);
            if (prefabRoot != null)
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(inv);
                PrefabUtility.RecordPrefabInstancePropertyModifications(bootstrap);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = inv.gameObject;
            Debug.Log(
                $"[Bow] Added Short Bow ×1, Stone Arrow ×{StoneQty}, Steel Arrow ×{SteelQty} to carried inventory. "
                + "Enter Play to auto-equip bow + stone (steel stays in bag).");
        }

        [MenuItem("JRogue/Inventory/Place Bow Kit in SampleScene")]
        public static void PlaceBowKitInSampleScene()
        {
            GameObject bowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BowPrefabPath);
            GameObject stonePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StonePrefabPath);
            GameObject steelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SteelPrefabPath);
            if (bowPrefab == null || stonePrefab == null || steelPrefab == null)
            {
                Debug.LogError("[Bow] Missing world item prefabs.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Transform existing = FindPlacedRoot(scene);
            if (existing != null)
            {
                Debug.Log("[Bow] Bow kit pickups already in SampleScene.");
                Selection.activeGameObject = existing.gameObject;
                return;
            }

            GameObject root = new GameObject(PlacedRootName);
            Undo.RegisterCreatedObjectUndo(root, "Place Bow Kit");
            SceneManager.MoveGameObjectToScene(root, scene);

            PlacePickup(bowPrefab, scene, root.transform, new Vector3(4f, -0.5f, 0f), "WorldItem_ShortBow (Sample)");
            PlacePickup(stonePrefab, scene, root.transform, new Vector3(4.5f, -0.5f, 0f), "WorldItem_StoneArrow (Sample)");
            PlacePickup(steelPrefab, scene, root.transform, new Vector3(5f, -0.5f, 0f), "WorldItem_SteelArrow (Sample)");

            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = root;
            Debug.Log("[Bow] Placed bow + arrow world pickups under BowKit (Sample).");
        }

        static void PlacePickup(
            GameObject prefab,
            Scene scene,
            Transform parent,
            Vector3 localPos,
            string instanceName)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = instanceName;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = localPos;
        }

        static Transform FindPlacedRoot(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == PlacedRootName)
                    return root.transform;
            }

            return null;
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
