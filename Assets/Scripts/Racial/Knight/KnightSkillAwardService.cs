using JRogue.Ability;
using JRogue.Ability.Knight;
using JRogue.Actors;
using JRogue.Racial.Knight;
using JRogue.Stats.Racial;
using UnityEngine;

namespace JRogue.Racial
{
    public static class KnightSkillAwardService
    {
        public static void AwardAfterSuccessfulUse(
            BaseActor actor,
            AbilityAction ability,
            KnightSkillProficiencyEventKind eventKind)
        {
            if (actor == null || ability == null)
                return;

            string skillId = ResolveSkillId(actor, ability);
            if (string.IsNullOrEmpty(skillId))
                return;

            KnightSkillProficiencyDispatcher.Dispatch(actor, skillId, eventKind, ability);
        }

        public static string ResolveSkillId(BaseActor actor, AbilityAction ability)
        {
            if (ability == null)
                return null;

            if (!string.IsNullOrEmpty(ability.knightSkillId))
                return ability.knightSkillId;

            if (ability is KnightAuraToggleAbility toggle)
                return toggle.NodeId;

            if (ability is KnightAuraPulseAbility pulse)
                return pulse.NodeId;

            HumanClassSkillTreeRuntime tree = actor?.GetComponent<HumanClassSkillTreeRuntime>();
            if (tree?.SkillTree == null)
                return null;

            foreach (HumanClassSkillTreeNodeData node in tree.SkillTree.nodes)
            {
                if (node?.activeAbilities == null)
                    continue;

                for (int i = 0; i < node.activeAbilities.Count; i++)
                {
                    if (node.activeAbilities[i] == ability)
                        return node.nodeId;
                }
            }

            return null;
        }
    }
}
