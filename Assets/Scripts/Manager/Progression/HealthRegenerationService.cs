using System.Collections.Generic;
using UnityEngine;

namespace JRogue.Manager.Progression
{
    /// <summary>HP recovery rate during rest steps. See Docs/Progression/Rest-Requirements.md §6.3.</summary>
    public static class HealthRegenerationService
    {
        public const int DefaultHpRegenPerRestStep = 1;

        static readonly Dictionary<GameObject, float> FlatModifiersByActor = new Dictionary<GameObject, float>();

        public static void RegisterFlatModifier(GameObject actor, float delta, object source)
        {
            if (actor == null || source == null)
                return;

            if (!FlatModifiersByActor.ContainsKey(actor))
                FlatModifiersByActor[actor] = 0f;
            FlatModifiersByActor[actor] += delta;
        }

        public static void UnregisterModifiersFromSource(GameObject actor, object source)
        {
            if (actor == null)
                return;
            FlatModifiersByActor.Remove(actor);
        }

        public static int ComputeEffectiveHpRegenPerStep(GameObject actor)
        {
            float mods = FlatModifiersByActor.TryGetValue(actor, out float m) ? m : 0f;
            return Mathf.Max(0, Mathf.RoundToInt(DefaultHpRegenPerRestStep + mods));
        }
    }
}
