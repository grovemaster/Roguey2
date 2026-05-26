using JRogue.Stats;
using JRogue.Stats.Racial;

namespace JRogue.Racial
{
    public static class HumanClassSkillTreePayloadApplicator
    {
        public static void ApplyRankedStats(
            CharacterStats stats,
            HumanClassSkillNodeModifierSource source,
            HumanClassSkillTreeNodeData node,
            int rank)
        {
            if (stats == null || source == null || node == null || rank < 1)
                return;

            RemoveRankedStats(stats, source, node);

            if (node.perRankStatModifiers == null)
                return;

            foreach (HumanPerRankStatModifier mod in node.perRankStatModifiers)
            {
                Stat targetStat = stats.GetStatByType(mod.attribute);
                targetStat?.AddModifier(mod.valuePerRank * rank, source, ModifierSourceLayer.RacialProgression);
            }
        }

        public static void RemoveRankedStats(
            CharacterStats stats,
            HumanClassSkillNodeModifierSource source,
            HumanClassSkillTreeNodeData node)
        {
            if (stats == null || source == null || node?.perRankStatModifiers == null)
                return;

            foreach (HumanPerRankStatModifier mod in node.perRankStatModifiers)
            {
                Stat targetStat = stats.GetStatByType(mod.attribute);
                targetStat?.RemoveModifiersFromSource(source);
            }
        }
    }
}
