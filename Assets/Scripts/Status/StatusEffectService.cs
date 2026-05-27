using JRogue.Actors;
using JRogue.Stats;
using UnityEngine;

namespace JRogue.Status
{
    public static class StatusEffectService
    {
        public static System.Func<int, int, int> RangeRoll = Random.Range;

        public static bool TryApply(
            BaseActor target,
            StatusEffectDefinition definition,
            GameObject source = null,
            int stacks = 1)
        {
            if (target == null || definition == null || definition.statusId == StatusEffectId.None)
                return false;

            StatusEffectController controller = target.GetComponent<StatusEffectController>();
            if (controller == null)
                return false;

            return TryApply(controller, definition, source, stacks);
        }

        internal static bool TryApply(
            StatusEffectController controller,
            StatusEffectDefinition definition,
            GameObject source = null,
            int stacks = 1)
        {
            if (controller == null || definition == null || definition.statusId == StatusEffectId.None)
                return false;

            if (stacks <= 0)
                stacks = 1;

            if (IsImmune(controller, definition))
            {
                Debug.Log($"[Status] {controller.DisplayName} is immune to {definition.displayName}.");
                return false;
            }

            int duration = Mathf.Max(1, definition.maxDurationTurns);
            if (controller.TryGetStatus(definition.statusId, out StatusEffectInstance existing))
            {
                existing.turnsRemaining = duration;
                existing.source = source;
                Debug.Log($"[Status] {controller.DisplayName} refreshed {definition.displayName} ({duration} turns).");
                return true;
            }

            controller.AddStatus(new StatusEffectInstance
            {
                definition = definition,
                source = source,
                turnsRemaining = duration
            });

            Debug.Log($"[Status] {controller.DisplayName} is now {definition.displayName} ({duration} turns).");
            return true;
        }

        static bool IsImmune(StatusEffectController controller, StatusEffectDefinition definition)
        {
            StatusImmunity immunity = controller.GetComponent<StatusImmunity>();
            if (immunity != null && immunity.IsImmuneTo(definition.statusId))
                return true;

            if (definition.statusId == StatusEffectId.Poisoned && !definition.ignoresPoisonImmunity)
            {
                CharacterStats stats = controller.Stats;
                if (stats != null && stats.race == Race.Undead)
                    return true;
            }

            return false;
        }

        public static int RollD20() => Mathf.Clamp(RangeRoll(1, 21), 1, 20);
    }
}
