#if UNITY_EDITOR
using JRogue.World.Lighting;
using UnityEditor;
using UnityEngine;

namespace JRogue.Editor.Lighting
{
    [CustomEditor(typeof(LightingScenarioController))]
    public sealed class LightingScenarioControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();

            var controller = (LightingScenarioController)target;
            var scenarios = controller.Scenarios;

            if (scenarios == null || scenarios.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No scenarios assigned. Run JRogue/Lighting/Bootstrap SampleScene Lighting Harness " +
                    "(or Create QA Lighting Scenario Pack) with SampleScene open.",
                    MessageType.Info);
                if (GUILayout.Button("Bootstrap SampleScene Lighting Harness"))
                    LightingScenarioSampleSceneBootstrap.BootstrapActiveSceneLightingHarness();
                return;
            }

            EditorGUILayout.LabelField("Scenario Quick Actions", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Apply Active"))
                {
                    Undo.RecordObject(controller, "Apply Active Lighting Scenario");
                    controller.ApplyScenarioByIndex(controller.ActiveScenarioIndex);
                    EditorUtility.SetDirty(controller);
                }

                if (GUILayout.Button("Prev"))
                {
                    Undo.RecordObject(controller, "Apply Previous Lighting Scenario");
                    controller.ApplyPreviousScenario();
                    EditorUtility.SetDirty(controller);
                }

                if (GUILayout.Button("Next"))
                {
                    Undo.RecordObject(controller, "Apply Next Lighting Scenario");
                    controller.ApplyNextScenario();
                    EditorUtility.SetDirty(controller);
                }
            }

            EditorGUILayout.Space(4f);
            for (int i = 0; i < scenarios.Count; i++)
            {
                LightingScenarioDefinition scenario = scenarios[i];
                if (scenario == null)
                    continue;

                using (new EditorGUILayout.HorizontalScope())
                {
                    bool isActive = i == controller.ActiveScenarioIndex;
                    GUI.enabled = !isActive;
                    if (GUILayout.Button($"Apply {scenario.DisplayName}"))
                    {
                        Undo.RecordObject(controller, "Apply Lighting Scenario");
                        controller.ApplyScenarioByIndex(i);
                        EditorUtility.SetDirty(controller);
                    }

                    GUI.enabled = true;
                    GUILayout.Label(isActive ? "ACTIVE" : string.Empty, GUILayout.Width(50f));
                }
            }
        }
    }
}
#endif
