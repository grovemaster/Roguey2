using UnityEngine;

namespace JRogue.World.Lighting
{
    [CreateAssetMenu(
        menuName = "JRogue/Lighting/Lighting Scenario Definition",
        fileName = "LightingScenario_")]
    public sealed class LightingScenarioDefinition : ScriptableObject
    {
        [SerializeField] string scenarioId = "Phase1_Core";
        [SerializeField] string displayName = "Phase 1 - Core";
        [TextArea(2, 8)]
        [SerializeField] string description =
            "Short scenario notes and expected results.";

        [Tooltip("Scene root object names to activate for this phase.")]
        [SerializeField] string[] activeRootNames;

        [TextArea(2, 12)]
        [SerializeField] string[] validationChecklist;

        public string ScenarioId => scenarioId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? scenarioId : displayName;
        public string Description => description;
        public string[] ActiveRootNames => activeRootNames;
        public string[] ValidationChecklist => validationChecklist;
    }
}
