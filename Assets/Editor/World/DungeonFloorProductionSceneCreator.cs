#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JRogue.Editor.World
{
    /// <summary>
    /// Phase 1 production dungeon shell: scene path, build settings, and routing validation.
    /// </summary>
    public static class DungeonFloorProductionSceneCreator
    {
        public const string ProductionSceneFolder = "Assets/Scenes/Dungeon/DungeonFloor";
        public const string ProductionScenePath = ProductionSceneFolder + "/DungeonFloor.unity";
        public const string LegacyProductionScenePath = "Assets/Scenes/Dungeon/DungeonFloor.unity";

        static readonly string[] RequiredBuildScenes =
        {
            "Assets/Scenes/Dungeon/DungeonFloorTest.unity",
            ProductionScenePath,
            "Assets/Scenes/Town/TownTest.unity",
            "Assets/Scenes/Town/DimensionSquareTest.unity",
            "Assets/Scenes/Town/DistrictTownTest.unity",
        };

        [MenuItem("JRogue/Dungeon/Phase 1 — Setup Production Dungeon")]
        public static void SetupProductionDungeonPhase1()
        {
            EnsureFolder(ProductionSceneFolder);

            if (!File.Exists(ProductionScenePath))
            {
                if (File.Exists(LegacyProductionScenePath))
                {
                    AssetDatabase.CopyAsset(LegacyProductionScenePath, ProductionScenePath);
                    AssetDatabase.Refresh();
                    Debug.Log($"[Dungeon] Copied legacy scene to {ProductionScenePath}.");
                }
                else if (File.Exists(DungeonV0aPackCreator.TestScenePath))
                {
                    DungeonV0aPackCreator.CreateDungeonFloorTestScene();
                    AssetDatabase.CopyAsset(DungeonV0aPackCreator.TestScenePath, ProductionScenePath);
                    AssetDatabase.Refresh();
                }
                else
                {
                    Debug.LogError("[Dungeon] No template scene found — run Create DungeonFloorTest Scene first.");
                    return;
                }
            }

            var scene = EditorSceneManager.OpenScene(ProductionScenePath, OpenSceneMode.Single);
            DungeonV0aPackCreator.FixProductionSceneHierarchyInPlace();
            EditorSceneManager.SaveScene(scene);

            EnsureBuildSettings();
            AssetDatabase.SaveAssets();

            Debug.Log(
                "[Dungeon] Phase 1 complete: production scene at Assets/Scenes/Dungeon/DungeonFloor/DungeonFloor.unity, " +
                "Build Settings updated. TownTest → DungeonFloorTest; other hubs → DungeonFloor.");
        }

        public static void EnsureBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool changed = false;

            foreach (string path in RequiredBuildScenes)
            {
                if (!File.Exists(path))
                {
                    Debug.LogWarning($"[Dungeon] Build settings skip missing scene: {path}");
                    continue;
                }

                if (ContainsScene(scenes, path))
                    continue;

                scenes.Add(new EditorBuildSettingsScene(path, true));
                changed = true;
                Debug.Log($"[Dungeon] Added to Build Settings: {path}");
            }

            if (changed)
                EditorBuildSettings.scenes = scenes.ToArray();
        }

        static bool ContainsScene(List<EditorBuildSettingsScene> scenes, string path)
        {
            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path == path)
                    return true;
            }

            return false;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
