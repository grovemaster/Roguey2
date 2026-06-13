using JRogue.Stats;
using JRogue.Stats.Racial;

namespace JRogue.Progression.Proficiency
{
    public static class ProficiencyEligibility
    {
        public static bool CanTrain(CharacterStats stats, ProficiencyKind kind)
        {
            if (stats == null || kind == ProficiencyKind.None)
                return false;

            if (ProficiencyKindMapping.IsArcaneSchool(kind))
                return stats.race == Race.Human && stats.humanClass == HumanClass.Mage;

            if (ProficiencyKindMapping.IsDivineSchool(kind))
                return stats.race == Race.Human && stats.humanClass == HumanClass.Priest;

            if (kind == ProficiencyKind.DraconicSpellcraft)
                return stats.race == Race.Dragonian;

            if (kind == ProficiencyKind.Invocations)
                return false;

            return true;
        }

        public static string GetIneligibilityReason(CharacterStats stats, ProficiencyKind kind)
        {
            if (CanTrain(stats, kind))
                return string.Empty;

            if (ProficiencyKindMapping.IsArcaneSchool(kind))
                return "Only a Human Mage can train this proficiency.";

            if (ProficiencyKindMapping.IsDivineSchool(kind))
                return "Only a Human Priest can train this proficiency.";

            if (kind == ProficiencyKind.DraconicSpellcraft)
                return "Only a Dragonian can train Draconic Spellcraft.";

            return "This proficiency is unavailable.";
        }
    }
}
