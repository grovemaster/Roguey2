using JRogue.Stats;
using JRogue.Stats.Racial;

namespace JRogue.Racial
{
    public static class PriestPietyPassiveApplicator
    {
        public static void ApplyBandPassives(
            CharacterStats stats,
            PriestPietyBandModifierSource source,
            PriestPietyBandData band)
        {
            if (stats == null || source == null || band?.passiveModifiers == null)
                return;

            for (int i = 0; i < band.passiveModifiers.Count; i++)
            {
                HumanPerRankStatModifier mod = band.passiveModifiers[i];
                Stat targetStat = stats.GetStatByType(mod.attribute);
                targetStat?.AddModifier(mod.valuePerRank, source, ModifierSourceLayer.RacialProgression);
            }
        }

        public static void RemoveBandPassives(CharacterStats stats, PriestPietyBandModifierSource source)
        {
            if (stats == null || source == null)
                return;

            RemoveFromStat(stats.Strength, source);
            RemoveFromStat(stats.Dexterity, source);
            RemoveFromStat(stats.Constitution, source);
            RemoveFromStat(stats.Intelligence, source);
            RemoveFromStat(stats.Wisdom, source);
            RemoveFromStat(stats.Charisma, source);
            RemoveFromStat(stats.Luck, source);
        }

        static void RemoveFromStat(Stat stat, PriestPietyBandModifierSource source) =>
            stat?.RemoveModifiersFromSource(source);
    }
}
