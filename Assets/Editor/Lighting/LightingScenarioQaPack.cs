#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using JRogue.World.Lighting;
using UnityEditor;
using UnityEngine;

namespace JRogue.Editor.Lighting
{
    /// <summary>Shared QA scenario asset creation used by pack menu and SampleScene bootstrap.</summary>
    internal static class LightingScenarioQaPack
    {
        internal const string ScenarioPath = "Assets/Data/Lighting/Scenarios";

        internal static readonly QaScenarioSpec[] Specs =
        {
            new QaScenarioSpec(
                "LightingScenario_Phase1_Core",
                "Phase1_Core",
                "Phase 1 - Core Math + Rendering",
                "Validate ambient-only rooms, one static emitter, and lit vs dark tile presentation.",
                "LightingPhase_Phase1_Core",
                new[]
                {
                    "Ambient-only bright room looks fully visible.",
                    "Ambient-only dark room shows dark tiles (in LOS but under threshold).",
                    "Single emitter lights nearby receiver cells with falloff."
                }),
            new QaScenarioSpec(
                "LightingScenario_Phase2_FogMemory",
                "Phase2_FogMemory",
                "Phase 2 - Fog + Lighting Snapshot",
                "Validate that explored memory freezes lighting until cells are seen again.",
                "LightingPhase_Phase2_FogMemory",
                new[]
                {
                    "Seen lit cell turns explored after leaving LOS.",
                    "Off-screen emission change does not update explored memory.",
                    "Re-entering LOS refreshes memory from live lighting."
                }),
            new QaScenarioSpec(
                "LightingScenario_Phase3_RuntimeEmitters",
                "Phase3_RuntimeEmitters",
                "Phase 3 - Runtime Emission Changes",
                "Validate unlit->lit torch transitions and recompute triggers.",
                "LightingPhase_Phase3_RuntimeEmitters",
                new[]
                {
                    "Unlit wall torch can be ignited by allowed interaction.",
                    "LightingService.SetEmission updates visibility immediately.",
                    "Turn/action refresh updates dark/lit boundaries."
                }),
            new QaScenarioSpec(
                "LightingScenario_Phase4_CarriedLight",
                "Phase4_CarriedLight",
                "Phase 4 - Carried Light + Party Union",
                "Validate virtual emitters on party members and union of lit-visible sets.",
                "LightingPhase_Phase4_CarriedLight",
                new[]
                {
                    "Torch aura follows bearer while moving.",
                    "Multiple members produce union visible region.",
                    "Underlit enemies are hidden on dark tiles."
                }),
            new QaScenarioSpec(
                "LightingScenario_Phase5_DayNight",
                "Phase5_DayNight",
                "Phase 5 - Day/Night Ambient Cycle",
                "Validate ambient region phase changes over turn boundaries.",
                "LightingPhase_Phase5_DayNight",
                new[]
                {
                    "Ambient transitions occur on configured turn cadence.",
                    "Dark/bright presentation changes with ambient phase.",
                    "Cycle logging and recompute triggers fire as expected."
                }),
            new QaScenarioSpec(
                "LightingScenario_Phase6_EnemyAlert",
                "Phase6_EnemyAlert",
                "Phase 6 - Enemy Alert from Light",
                "Validate enemy alert from party light source detection separate from body visibility.",
                "LightingPhase_Phase6_EnemyAlert",
                new[]
                {
                    "Enemy enters alert from party light source in cone/LOS.",
                    "Alert occurs even when actor body is not yet visible.",
                    "No false alert when no party emitter is active."
                })
        };

        internal readonly struct QaScenarioSpec
        {
            public readonly string AssetName;
            public readonly string ScenarioId;
            public readonly string DisplayName;
            public readonly string Description;
            public readonly string RootName;
            public readonly string[] Checklist;

            public QaScenarioSpec(
                string assetName,
                string scenarioId,
                string displayName,
                string description,
                string rootName,
                string[] checklist)
            {
                AssetName = assetName;
                ScenarioId = scenarioId;
                DisplayName = displayName;
                Description = description;
                RootName = rootName;
                Checklist = checklist;
            }
        }

        internal static List<LightingScenarioDefinition> EnsureQaScenarioPack()
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Data/Lighting/Scenarios"));
            AssetDatabase.Refresh();

            var list = new List<LightingScenarioDefinition>(Specs.Length);
            for (int i = 0; i < Specs.Length; i++)
            {
                QaScenarioSpec spec = Specs[i];
                list.Add(CreateOrUpdateScenario(spec));
            }

            AssetDatabase.SaveAssets();
            return list;
        }

        static LightingScenarioDefinition CreateOrUpdateScenario(QaScenarioSpec spec)
        {
            string path = $"{ScenarioPath}/{spec.AssetName}.asset";
            LightingScenarioDefinition scenario =
                AssetDatabase.LoadAssetAtPath<LightingScenarioDefinition>(path);

            if (scenario == null)
            {
                scenario = ScriptableObject.CreateInstance<LightingScenarioDefinition>();
                AssetDatabase.CreateAsset(scenario, path);
            }

            var so = new SerializedObject(scenario);
            so.FindProperty("scenarioId").stringValue = spec.ScenarioId;
            so.FindProperty("displayName").stringValue = spec.DisplayName;
            so.FindProperty("description").stringValue = spec.Description;
            SetStringArray(so.FindProperty("activeRootNames"), new[] { spec.RootName });
            SetStringArray(so.FindProperty("validationChecklist"), spec.Checklist);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(scenario);
            return scenario;
        }

        static void SetStringArray(SerializedProperty prop, string[] values)
        {
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).stringValue = values[i];
        }
    }
}
#endif
