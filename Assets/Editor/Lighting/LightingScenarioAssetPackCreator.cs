#if UNITY_EDITOR
using JRogue.World.Lighting;
using UnityEditor;
using UnityEngine;

namespace JRogue.Editor.Lighting
{
    public static class LightingScenarioAssetPackCreator
    {
        const string CreateMenuPath = "Assets/Create/JRogue/Lighting/Create QA Lighting Scenario Pack";
        const string TopMenuPath = "JRogue/Lighting/Create QA Lighting Scenario Pack";

        [MenuItem(CreateMenuPath, false, 0)]
        [MenuItem(TopMenuPath, false, 0)]
        public static void CreateQaLightingScenarioPack()
        {
            LightingScenarioQaPack.EnsureQaScenarioPack();
            AssetDatabase.Refresh();

            Object first = AssetDatabase.LoadAssetAtPath<Object>(
                LightingScenarioQaPack.ScenarioPath + "/LightingScenario_Phase1_Core.asset");
            Selection.activeObject = first;
            Debug.Log("[Lighting:Scenario] QA scenario pack created under Assets/Data/Lighting/Scenarios.");
        }
    }
}
#endif
