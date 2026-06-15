using System.Collections.Generic;
using JRogue.Ability;
using JRogue.Actors;
using JRogue.Stats;
using JRogue.Stats.Racial;
using UnityEngine;

namespace JRogue.Racial
{
    public static class HumanPriestInvocationExecutionService
    {
        public static bool TryExecute(
            BaseActor actor,
            AbilityAction ability,
            Vector3Int? targetTile)
        {
            if (actor == null || ability == null)
                return false;

            CharacterStats stats = actor.stats;
            if (stats == null || stats.humanClass != HumanClass.Priest)
                return false;

            HumanPriestDevotionRuntime devotion = actor.GetComponent<HumanPriestDevotionRuntime>();
            if (devotion == null)
                return false;

            PriestInvocationDefinition invocation = FindEquippedInvocation(devotion, ability);
            if (invocation == null)
                return false;

            if (stats.currentDivinePower < invocation.divinePowerCost)
            {
                Debug.Log(HumanClassAbilityResources.InsufficientResourceMessage(HumanClass.Priest));
                return false;
            }

            if (invocation.pietyInvokeCost > 0)
            {
                HumanPriestCovenantRuntime covenant = actor.GetComponent<HumanPriestCovenantRuntime>();
                if (covenant == null || covenant.Piety < invocation.pietyInvokeCost)
                {
                    Debug.Log("Not enough piety for that invocation.");
                    return false;
                }
            }

            GameObject user = actor.gameObject;
            bool ok = targetTile.HasValue
                ? ability.Execute(user, targetTile.Value)
                : ability.Execute(user);

            if (!ok)
                return false;

            stats.currentDivinePower -= invocation.divinePowerCost;

            if (invocation.pietyInvokeCost > 0)
            {
                HumanPriestCovenantRuntime covenant = actor.GetComponent<HumanPriestCovenantRuntime>();
                covenant?.ApplyPietyLoss(invocation.pietyInvokeCost, "invoke", "Invocation offering.");
            }

            return true;
        }

        static PriestInvocationDefinition FindEquippedInvocation(
            HumanPriestDevotionRuntime devotion,
            AbilityAction ability)
        {
            if (devotion == null || ability == null)
                return null;

            IReadOnlyList<PriestInvocationDefinition> equipped = devotion.EquippedInvocations;
            for (int i = 0; i < equipped.Count; i++)
            {
                PriestInvocationDefinition invocation = equipped[i];
                if (invocation?.ability == ability)
                    return invocation;
            }

            return null;
        }
    }
}
