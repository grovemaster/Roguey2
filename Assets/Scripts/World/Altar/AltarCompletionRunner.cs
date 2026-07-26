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

                if (!CanRunEffects(instance, rule, out string denyReason))
                {
                    if (!string.IsNullOrEmpty(denyReason))
                        JRogue.UI.Gameplay.GameLogService.ActiveSession.Append(denyReason);
                    return;
                }

                instance.MarkRuleFired(ruleId);
                instance.ClearOfferings();
                RunEffects(instance, rule);
                return;
            }
        }

        static bool CanRunEffects(AltarInstance instance, AltarCompletionRule rule, out string denyReason)
        {
            denyReason = null;
            AltarCompletionEffect[] effects = rule.effects;
            if (effects == null)
                return true;

            for (int i = 0; i < effects.Length; i++)
            {
                AltarCompletionEffect effect = effects[i];
                if (effect == null)
                    continue;
                if (!effect.CanExecute(instance, out denyReason))
                    return false;
            }

            return true;
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
