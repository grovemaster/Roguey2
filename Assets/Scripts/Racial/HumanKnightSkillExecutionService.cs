using JRogue.Ability;
using JRogue.Ability.Knight;
using JRogue.Actors;
using JRogue.Racial.Knight;
using UnityEngine;

namespace JRogue.Racial
{
    public static class HumanKnightSkillExecutionService
    {
        public static bool TryExecute(
            BaseActor actor,
            AbilityAction ability,
            Vector3Int? targetTile,
            out KnightSkillProficiencyEventKind eventKind)
        {
            eventKind = KnightSkillProficiencyEventKind.Activation;
            if (actor == null || ability == null)
                return false;

            bool deactivatingToggle = IsDeactivatingToggle(actor, ability);

            GameObject user = actor.gameObject;
            bool ok = targetTile.HasValue
                ? ability.Execute(user, targetTile.Value)
                : ability.Execute(user);

            if (!ok)
                return false;

            if (deactivatingToggle)
                return true;

            if (!HumanClassAbilityResources.TrySpend(actor.stats, ability))
                return false;

            eventKind = ResolveEventKind(ability);
            KnightSkillAwardService.AwardAfterSuccessfulUse(actor, ability, eventKind);
            return true;
        }

        static bool IsDeactivatingToggle(BaseActor actor, AbilityAction ability)
        {
            if (actor == null || ability is not KnightAuraToggleAbility toggle)
                return false;

            string nodeId = toggle.NodeId;
            if (string.IsNullOrEmpty(nodeId))
                return false;

            KnightAuraStateRuntime auraState = actor.GetComponent<KnightAuraStateRuntime>();
            return auraState != null && auraState.IsActive(nodeId);
        }

        static KnightSkillProficiencyEventKind ResolveEventKind(AbilityAction ability)
        {
            if (ability is KnightAuraPulseAbility)
                return KnightSkillProficiencyEventKind.Pulse;

            return KnightSkillProficiencyEventKind.Activation;
        }
    }
}
