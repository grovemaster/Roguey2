using UnityEngine;

namespace JRogue.World.Altar
{
    public static class AltarCompletionRunner
    {
        public static void TryFireCompletion(AltarInstance instance)
        {
            if (instance?.Definition == null)
                return;

            AltarCompletionRule[] rules = instance.Definition.completionRules;
            if (rules == null)
                return;

            for (int i = 0; i < rules.Length; i++)
            {
                AltarCompletionRule rule = rules[i];
                if (rule == null)
                    continue;

                string ruleId = rule.ruleId ?? string.Empty;
                if (instance.IsRuleFired(ruleId))
                    continue;

                if (!AltarCompletionEvaluator.IsRuleSatisfied(instance, rule))
                    continue;

                instance.MarkRuleFired(ruleId);
                instance.ClearOfferings();
                RunEffects(instance, rule);
                return;
            }
        }

        static void RunEffects(AltarInstance instance, AltarCompletionRule rule)
        {
            AltarCompletionEffect[] effects = rule.effects;
            if (effects == null)
                return;

            for (int i = 0; i < effects.Length; i++)
            {
                AltarCompletionEffect effect = effects[i];
                if (effect == null)
                    continue;

                effect.Execute(instance);
            }
        }
    }
}
