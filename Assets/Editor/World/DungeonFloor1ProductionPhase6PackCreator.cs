#if UNITY_EDITOR
using System.IO;
using System.Text;
using JRogue.World.Generation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JRogue.Editor.World
{
    /// <summary>
    /// Phase 6: production QA validation, test-scene regression guard, playtest checklist (§12 AC6).
    /// </summary>
    public static class DungeonFloor1ProductionPhase6PackCreator
    {
        const string MenuPath = "JRogue/Dungeon/Phase 6 — Validate Production QA";

        [MenuItem(MenuPath, false, 56)]
        public static void ValidateProductionQaPhase6()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("[Floor1Production] Phase 6 cancelled — save or discard open scene changes first.");
                return;
            }

            var report = new StringBuilder();
            report.AppendLine("[Floor1Production] Phase 6 — production QA validation");
            int failures = 0;

            failures += ValidateRequiredAssets(report);
            failures += ValidateProductionScene(report);
            failures += ValidateTestSceneRegression(report);
            failures += ValidateBuildSettings(report);

            AppendPlaytestChecklist(report);

            if (failures == 0)
            {
                report.AppendLine("All automated checks PASSED. Complete the manual playtest checklist above.");
                Debug.Log(report.ToString());
            }
            else
            {
                report.AppendLine($"FAILED — {failures} automated check group(s) need attention.");
                Debug.LogError(report.ToString());
            }

            AssetDatabase.SaveAssets();
        }

        static int ValidateRequiredAssets(StringBuilder report)
        {
            int failures = 0;
            report.AppendLine("--- Required assets ---");

            if (!AssetExists(DungeonFloor1ProductionPhase2PackCreator.FloorProdPath))
            {
                report.AppendLine($"MISSING {DungeonFloor1ProductionPhase2PackCreator.FloorProdPath}");
                failures++;
            }

            if (!AssetExists(DungeonFloor1ProductionPhase2PackCreator.CatalogProdPath))
            {
                report.AppendLine($"MISSING {DungeonFloor1ProductionPhase2PackCreator.CatalogProdPath}");
                failures++;
            }

            var prodFloor = AssetDatabase.LoadAssetAtPath<DungeonFloorDefinition>(
                DungeonFloor1ProductionPhase2PackCreator.FloorProdPath);
            if (prodFloor != null && prodFloor.BaseDayNightCycles != 4)
            {
                report.AppendLine(
                    $"WARN production floor cycles = {prodFloor.BaseDayNightCycles} (expected 4 per §9.8).");
                failures++;
            }

            if (failures == 0)
                report.AppendLine("OK — production floor asset + catalog present; 4-cycle override verified.");

            return failures;
        }

        static int ValidateProductionScene(StringBuilder report)
        {
            report.AppendLine("--- Production scene (DungeonFloor) ---");

            if (!File.Exists(DungeonFloorProductionSceneCreator.ProductionScenePath))
            {
                report.AppendLine($"MISSING scene at {DungeonFloorProductionSceneCreator.ProductionScenePath}");
                report.AppendLine("Run JRogue → Dungeon → Phase 1 — Setup Production Dungeon.");
                return 1;
            }

            var previousPath = EditorSceneManager.GetActiveScene().path;
            var scene = EditorSceneManager.OpenScene(
                DungeonFloorProductionSceneCreator.ProductionScenePath,
                OpenSceneMode.Single);

            DungeonV0aPackCreator.FixProductionSceneHierarchyInPlace();
            EditorSceneManager.SaveScene(scene);

            bool ok = new DungeonProductionSceneValidator().ValidateScene();
            report.AppendLine(ok
                ? "OK — DungeonFloorRuntime present; no test Generate controller."
                : "FAIL — production scene hierarchy incomplete.");

            if (!string.IsNullOrEmpty(previousPath) && previousPath != scene.path)
                EditorSceneManager.OpenScene(previousPath, OpenSceneMode.Single);

            return ok ? 0 : 1;
        }

        static int ValidateTestSceneRegression(StringBuilder report)
        {
            report.AppendLine("--- Test scene regression (DungeonFloorTest) ---");

            if (!File.Exists(DungeonV0aPackCreator.TestScenePath))
            {
                report.AppendLine($"MISSING {DungeonV0aPackCreator.TestScenePath}");
                return 1;
            }

            string yaml = File.ReadAllText(DungeonV0aPackCreator.TestScenePath);
            bool hasTestController = yaml.Contains(
                "JRogue::JRogue.World.Generation.DungeonFloorTestController");
            bool hasProductionRuntime = yaml.Contains(
                "JRogue::JRogue.World.Generation.DungeonFloorRuntime");

            if (!hasTestController || hasProductionRuntime)
            {
                report.AppendLine(
                    "FAIL — DungeonFloorTest must keep DungeonFloorTestController (Generate + two-floor persist).");
                report.AppendLine("Run JRogue → Dungeon → Fix DungeonFloorTest Scene.");
                return 1;
            }

            report.AppendLine("OK — test scene still uses DungeonFloorTestController.");
            return 0;
        }

        static int ValidateBuildSettings(StringBuilder report)
        {
            report.AppendLine("--- Build Settings ---");

            DungeonFloorProductionSceneCreator.EnsureBuildSettings();

            int failures = 0;
            foreach (string path in new[]
                     {
                         DungeonV0aPackCreator.TestScenePath,
                         DungeonFloorProductionSceneCreator.ProductionScenePath,
                         "Assets/Scenes/Town/DimensionSquareTest.unity",
                     })
            {
                if (!File.Exists(path))
                {
                    report.AppendLine($"MISSING scene file: {path}");
                    failures++;
                    continue;
                }

                if (!IsSceneEnabledInBuildSettings(path))
                {
                    report.AppendLine($"NOT in Build Settings: {path}");
                    failures++;
                }
            }

            if (failures == 0)
                report.AppendLine("OK — test, production, and hub scenes enabled in Build Settings.");

            return failures > 0 ? 1 : 0;
        }

        static void AppendPlaytestChecklist(StringBuilder report)
        {
            report.AppendLine("--- Manual playtest checklist (AC6-1) ---");
            report.AppendLine("1. DimensionSquareTest → dungeon portal → Floor 1 generates; party in luminescent_cavern.");
            report.AppendLine("2. Reach Floor 2 via north-edge portal; return to Floor 1 — layout/enemies unchanged.");
            report.AppendLine("3. Let dungeon time expire (4 cycles) → modal → return to DimensionSquareTest.");
            report.AppendLine("4. Survivors at full HP/SP; inventory kept; dead members stay dead.");
            report.AppendLine("5. Town day advances + Day phase; dungeon portal closed until next window.");
            report.AppendLine("6. TownTest → DungeonFloorTest still works (Generate, two-floor persist unchanged).");
        }

        static bool AssetExists(string assetPath) =>
            !string.IsNullOrEmpty(assetPath) && File.Exists(assetPath);

        static bool IsSceneEnabledInBuildSettings(string scenePath)
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].path == scenePath && scenes[i].enabled)
                    return true;
            }

            return false;
        }
    }
}
#endif
