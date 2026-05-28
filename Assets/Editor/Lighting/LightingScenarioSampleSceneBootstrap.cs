#if UNITY_EDITOR
using System.Collections.Generic;
using JRogue.World.Lighting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JRogue.Editor.Lighting
{
    public static class LightingScenarioSampleSceneBootstrap
    {
        const string LightingSystemObjectName = "LightingSystem";
        const string ScenarioRootPrefix = "LightingPhase_";

        const string BootstrapMenuPath = "JRogue/Lighting/Bootstrap SampleScene Lighting Harness";
        const string BootstrapAssetsMenuPath = "Assets/Create/JRogue/Lighting/Bootstrap SampleScene Lighting Harness";

        [MenuItem(BootstrapMenuPath, false, 1)]
        [MenuItem(BootstrapAssetsMenuPath, false, 1)]
        public static void BootstrapActiveSceneLightingHarness()
        {
            GameObject lightingSystem = FindOrCreateLightingSystem();
            EnsureLightingService(lightingSystem);
            LightingScenarioController controller = EnsureController(lightingSystem);

            List<LightingScenarioDefinition> scenarios = LightingScenarioQaPack.EnsureQaScenarioPack();
            EnsurePhaseRoots(lightingSystem.transform, scenarios);
            AssignScenarios(controller, scenarios);

            controller.ApplyScenarioByIndex(0);

            EditorUtility.SetDirty(lightingSystem);
            EditorUtility.SetDirty(controller);
            MarkActiveSceneDirty();
            Selection.activeGameObject = lightingSystem;

            Debug.Log(
                "[Lighting:Scenario] SampleScene harness ready on 'LightingSystem'. " +
                "Phase roots created; Phase 1 active. Place test geometry under each LightingPhase_* child.");
        }

        static GameObject FindOrCreateLightingSystem()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected != null && selected.name == LightingSystemObjectName)
                return selected;

            LightingScenarioController existing = Object.FindAnyObjectByType<LightingScenarioController>();
            if (existing != null)
                return existing.gameObject;

            GameObject byName = GameObject.Find(LightingSystemObjectName);
            if (byName != null)
                return byName;

            var created = new GameObject(LightingSystemObjectName);
            Undo.RegisterCreatedObjectUndo(created, "Create LightingSystem");
            Debug.Log("[Lighting:Scenario] Created LightingSystem GameObject.");
            return created;
        }

        static void EnsureLightingService(GameObject host)
        {
            if (host.GetComponent<LightingService>() == null)
                Undo.AddComponent<LightingService>(host);
        }

        static LightingScenarioController EnsureController(GameObject host)
        {
            LightingScenarioController controller = host.GetComponent<LightingScenarioController>();
            if (controller == null)
                controller = Undo.AddComponent<LightingScenarioController>(host);

            var so = new SerializedObject(controller);
            so.FindProperty("scenarioRootPrefix").stringValue = ScenarioRootPrefix;
            so.FindProperty("refreshVisibilityAfterApply").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            return controller;
        }

        static void EnsurePhaseRoots(Transform parent, List<LightingScenarioDefinition> scenarios)
        {
            var existingRoots = new HashSet<string>();
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name.StartsWith(ScenarioRootPrefix))
                    existingRoots.Add(child.name);
            }

            for (int i = 0; i < scenarios.Count; i++)
            {
                LightingScenarioDefinition scenario = scenarios[i];
                if (scenario == null)
                    continue;

                string rootName = GetRootName(scenario);
                if (existingRoots.Contains(rootName))
                    continue;

                var rootGo = new GameObject(rootName);
                Undo.RegisterCreatedObjectUndo(rootGo, "Create Lighting Phase Root");
                rootGo.transform.SetParent(parent, false);
                rootGo.transform.localPosition = Vector3.zero;
                rootGo.SetActive(false);
                existingRoots.Add(rootName);
            }
        }

        static string GetRootName(LightingScenarioDefinition scenario)
        {
            if (scenario.ActiveRootNames != null && scenario.ActiveRootNames.Length > 0
                && !string.IsNullOrWhiteSpace(scenario.ActiveRootNames[0]))
            {
                return scenario.ActiveRootNames[0];
            }

            return $"{ScenarioRootPrefix}{scenario.ScenarioId}";
        }

        static void AssignScenarios(
            LightingScenarioController controller,
            List<LightingScenarioDefinition> scenarios)
        {
            var so = new SerializedObject(controller);
            SerializedProperty list = so.FindProperty("scenarios");
            list.arraySize = scenarios.Count;
            for (int i = 0; i < scenarios.Count; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = scenarios[i];

            so.FindProperty("activeScenarioIndex").intValue = 0;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
        }

        static void MarkActiveSceneDirty()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
                return;

            EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
#endif
