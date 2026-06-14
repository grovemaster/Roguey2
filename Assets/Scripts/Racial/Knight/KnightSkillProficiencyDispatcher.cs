using JRogue.Actors;
using JRogue.Ability;
using JRogue.Progression.Proficiency;
using JRogue.Stats;
using JRogue.Stats.Racial;

namespace JRogue.Racial.Knight
{
    public static class KnightSkillProficiencyDispatcher
    {
        public static void Dispatch(
            BaseActor actor,
            string skillId,
            KnightSkillProficiencyEventKind eventKind,
            AbilityAction ability)
        {
            if (actor == null || string.IsNullOrEmpty(skillId))
                return;

            CharacterStats stats = actor.stats;
            if (stats == null
                || stats.race != Race.Human
                || stats.humanClass != HumanClass.Knight
                || stats.racialSubsystem != RacialSubsystemKind.HumanSpecialization)
            {
                return;
            }

            HumanClassSkillTreeRuntime treeRuntime = actor.GetComponent<HumanClassSkillTreeRuntime>();
            KnightSkillMasteryRuntime masteryRuntime = actor.GetComponent<KnightSkillMasteryRuntime>();
            if (treeRuntime == null || treeRuntime.SkillTree == null || masteryRuntime == null)
                return;

            if (!treeRuntime.SkillTree.TryFindNode(skillId, out HumanClassSkillTreeNodeData node))
                return;

            int rank = treeRuntime.GetRank(skillId);
            if (rank < 1)
                return;

            int basePxp = KnightSkillProgressionRules.ResolveBasePxp(node, ability);
            string masteryId = node.ResolveMasteryId();

            if (node.HasActiveAbilities && rank < node.maxRanks)
                ApplyRankPxp(actor, treeRuntime, masteryRuntime, skillId, rank, node.maxRanks, basePxp);

            ApplyMasteryPxp(stats, masteryRuntime, masteryId, basePxp);
        }

        static void ApplyRankPxp(
            BaseActor actor,
            HumanClassSkillTreeRuntime treeRuntime,
            KnightSkillMasteryRuntime masteryRuntime,
            string skillId,
            int currentRank,
            int maxRanks,
            int basePxp)
        {
            int pxp = masteryRuntime.GetRankPxp(skillId) + basePxp;
            int rank = currentRank;

            while (rank < maxRanks)
            {
                int threshold = KnightSkillProgressionRules.GetXpToNextRank(rank);
                if (pxp < threshold)
                    break;

                if (!treeRuntime.TryIncrementRankFromCombat(skillId, out _))
                    break;

                pxp -= threshold;
                rank = treeRuntime.GetRank(skillId);
            }

            masteryRuntime.SetRankPxp(skillId, pxp);
        }

        static void ApplyMasteryPxp(
            CharacterStats stats,
            KnightSkillMasteryRuntime masteryRuntime,
            string masteryId,
            int basePxp)
        {
            int trainingCap = KnightSkillProgressionRules.GetTrainingCap(stats.level);
            int level = masteryRuntime.GetMasteryLevel(masteryId);
            int pxp = masteryRuntime.GetMasteryPxp(masteryId) + basePxp;

            while (level < trainingCap && level < ProficiencyRules.MaxLevel)
            {
                int threshold = KnightSkillProgressionRules.GetXpToNextMastery(level);
                if (pxp < threshold)
                    break;

                pxp -= threshold;
                level++;
            }

            masteryRuntime.SetMastery(masteryId, level, pxp);
        }
    }
}
