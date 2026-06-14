using JRogue.Ability;
using JRogue.Actors;
using JRogue.Progression.Proficiency;
using JRogue.Stats.Racial;
using UnityEngine;

namespace JRogue.Racial
{
    public static class KnightSkillProgressionRules
    {
        public const int DefaultEventPxp = 12;

        public static int GetXpToNextRank(int currentRank) =>
            ProficiencyRules.GetBaseXpToNextLevel(Mathf.Max(0, currentRank));

        public static int GetXpToNextMastery(int currentMasteryLevel) =>
            ProficiencyRules.GetBaseXpToNextLevel(Mathf.Max(0, currentMasteryLevel));

        public static int GetTrainingCap(int characterLevel) =>
            ProficiencyRules.GetTrainingCap(characterLevel);

        public static int ResolveBasePxp(HumanClassSkillTreeNodeData node, AbilityAction ability)
        {
            if (ability != null && ability.proficiencyXpOverride > 0)
                return ability.proficiencyXpOverride;

            if (node != null && node.proficiencyXpOverride > 0)
                return node.proficiencyXpOverride;

            return DefaultEventPxp;
        }
    }
}
