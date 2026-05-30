#if UNITY_EDITOR
using JRogue.Controller.Enemy;
using JRogue.Interactables;
using JRogue.Spawn;
using UnityEditor;
using UnityEngine;

namespace JRogue.Editor.Interactables
{
    /// <summary>
    /// Creates or refreshes skeleton lever-spawn assets and wires Lever 1.
    /// </summary>
    public static class EnemySpawnAssetPackCreator
    {
        const string SpawnRoot = "Assets/Data/Spawn";
        const string EffectsPath = "Assets/Data/Interactables/Effects";
        const string LeverFirstPath = "Assets/Data/Interactables/LeverSwitch_First.asset";
        const string EnemyPrefabPath = "Assets/Prefabs/Actor/Enemy/Enemy.prefab";

        const string CreateMenuPath = "Assets/Create/JRogue/Interactables/Create Skeleton Lever Spawn Assets";
        const string TopMenuPath = "JRogue/Interactables/Create Skeleton Lever Spawn Assets";

        [MenuItem(CreateMenuPath, false, 50)]
        [MenuItem(TopMenuPath, false, 50)]
        public static void CreateSkeletonLeverSpawnAssets()
        {
            EnsureFolder(SpawnRoot);
            EnsureFolder(EffectsPath);

            var definition = LoadOrCreate<EnemySpawnDefinition>(
                $"{SpawnRoot}/Spawn_Skeleton_NorthOfLever.asset");
            definition.placementPolicy =
                EnemySpawnPlacementPolicy.NorthOfOriginThenNearestUnoccupiedFloor;
            definition.primaryOffset = new Vector3Int(0, 1, 0);
            definition.enemyPrefab = AssetDatabase.LoadAssetAtPath<EnemyController>(EnemyPrefabPath);
            EditorUtility.SetDirty(definition);

            var effect = LoadOrCreate<SpawnEnemyInteractableEffect>(
                $"{EffectsPath}/SpawnSkeletonOnLeverActivate.asset");
            effect.spawnDefinition = definition;
            EditorUtility.SetDirty(effect);

            var lever = AssetDatabase.LoadAssetAtPath<InteractableTileDefinition>(LeverFirstPath);
            if (lever != null)
            {
                lever.onActivateEffects = new InteractableEffect[] { effect };
                EditorUtility.SetDirty(lever);
            }
            else
            {
                Debug.LogWarning($"[EnemySpawn] Lever asset not found at {LeverFirstPath}. Wire effect manually.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[EnemySpawn] Created/refreshed spawn assets and wired LeverSwitch_First.");
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
                return existing;

            var created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, path);
            return created;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(name))
                AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
