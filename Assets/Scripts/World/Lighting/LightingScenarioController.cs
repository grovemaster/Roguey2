using System.Collections.Generic;
using JRogue.Manager.Visibility;
using UnityEngine;

namespace JRogue.World.Lighting
{
    /// <summary>
    /// Scene harness to quickly switch lighting test phases by enabling/disabling
    /// named root GameObjects under this controller.
    /// </summary>
    public sealed class LightingScenarioController : MonoBehaviour
    {
        [SerializeField] List<LightingScenarioDefinition> scenarios =
            new List<LightingScenarioDefinition>();

        [Tooltip("If a scenario doesn't specify roots, we try prefix+ScenarioId.")]
        [SerializeField] string scenarioRootPrefix = "LightingPhase_";

        [SerializeField, Min(0)] int activeScenarioIndex;
        [SerializeField] bool refreshVisibilityAfterApply = true;

        public IReadOnlyList<LightingScenarioDefinition> Scenarios => scenarios;
        public int ActiveScenarioIndex => activeScenarioIndex;

        public bool ApplyScenarioByIndex(int index)
        {
            if (index < 0 || index >= scenarios.Count || scenarios[index] == null)
                return false;

            activeScenarioIndex = index;
            LightingScenarioDefinition scenario = scenarios[index];
            ApplyScenarioRoots(scenario);
            RefreshVisibilityIfPresent();
            Debug.Log(
                $"[Lighting:Scenario] Applied {scenario.DisplayName} ({scenario.ScenarioId}) at index {index}.");
            return true;
        }

        public bool ApplyScenarioById(string scenarioId)
        {
            if (string.IsNullOrWhiteSpace(scenarioId))
                return false;

            for (int i = 0; i < scenarios.Count; i++)
            {
                LightingScenarioDefinition scenario = scenarios[i];
                if (scenario == null || scenario.ScenarioId != scenarioId)
                    continue;

                return ApplyScenarioByIndex(i);
            }

            Debug.LogWarning($"[Lighting:Scenario] Scenario id not found: {scenarioId}");
            return false;
        }

        public bool ApplyNextScenario()
        {
            if (scenarios.Count == 0)
                return false;

            int next = (activeScenarioIndex + 1) % scenarios.Count;
            return ApplyScenarioByIndex(next);
        }

        public bool ApplyPreviousScenario()
        {
            if (scenarios.Count == 0)
                return false;

            int prev = activeScenarioIndex - 1;
            if (prev < 0)
                prev = scenarios.Count - 1;
            return ApplyScenarioByIndex(prev);
        }

        [ContextMenu("Apply Active Lighting Scenario")]
        void ApplyActiveScenarioFromContextMenu()
        {
            if (!ApplyScenarioByIndex(activeScenarioIndex))
                Debug.LogWarning("[Lighting:Scenario] No valid active scenario to apply.");
        }

        [ContextMenu("Apply Next Lighting Scenario")]
        void ApplyNextScenarioFromContextMenu()
        {
            if (!ApplyNextScenario())
                Debug.LogWarning("[Lighting:Scenario] Could not apply next scenario.");
        }

        void ApplyScenarioRoots(LightingScenarioDefinition scenario)
        {
            HashSet<string> targetRoots = BuildTargetRootSet(scenario);
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                string childName = child.name;
                bool shouldManage = childName.StartsWith(scenarioRootPrefix);
                if (!shouldManage)
                    continue;

                bool shouldEnable = targetRoots.Contains(childName);
                if (child.gameObject.activeSelf != shouldEnable)
                    child.gameObject.SetActive(shouldEnable);
            }
        }

        HashSet<string> BuildTargetRootSet(LightingScenarioDefinition scenario)
        {
            var set = new HashSet<string>();
            if (scenario == null)
                return set;

            string[] explicitRoots = scenario.ActiveRootNames;
            if (explicitRoots != null && explicitRoots.Length > 0)
            {
                for (int i = 0; i < explicitRoots.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(explicitRoots[i]))
                        set.Add(explicitRoots[i]);
                }
            }

            if (set.Count == 0)
                set.Add($"{scenarioRootPrefix}{scenario.ScenarioId}");
            return set;
        }

        void RefreshVisibilityIfPresent()
        {
            if (!refreshVisibilityAfterApply || !Application.isPlaying)
                return;

            VisibilityManager visibility = FindAnyObjectByType<VisibilityManager>();
            if (visibility == null)
                return;

            visibility.ResetForNewFloor();
            visibility.RefreshPartyVision();
        }
    }
}
