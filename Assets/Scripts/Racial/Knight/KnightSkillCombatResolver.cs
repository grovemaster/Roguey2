using UnityEngine;

namespace JRogue.Racial
{
    public static class KnightSkillCombatResolver
    {
        public static float GetPartyDamageBonusPercent(
            HumanClassSkillTreeNodeData node,
            int treeRank,
            int masteryLevel)
        {
            if (node == null || treeRank < 1)
                return 0f;

            float rankBonus = node.effectPercentPerRank * treeRank;
            float masteryMultiplier = 1f + masteryLevel * 0.02f;
            return rankBonus * masteryMultiplier;
        }

        public static int GetSoulPowerUpkeep(
            HumanClassSkillTreeNodeData node,
            int treeRank,
            int masteryLevel)
        {
            if (node == null || treeRank < 1)
                return 0;

            int rankUpkeep = node.soulPowerUpkeepPerRank * treeRank;
            int masteryReduction = masteryLevel / 5;
            return Mathf.Max(1, rankUpkeep - masteryReduction);
        }

        public static float GetDamageReductionPercent(
            HumanClassSkillTreeNodeData node,
            int treeRank,
            int masteryLevel)
        {
            if (node == null || treeRank < 1)
                return 0f;

            float rankBonus = node.effectPercentPerRank * treeRank;
            return rankBonus + masteryLevel * 0.5f;
        }

        public static float GetPulsePotencyPercent(
            HumanClassSkillTreeNodeData node,
            int treeRank,
            int masteryLevel)
        {
            if (node == null || treeRank < 1)
                return 0f;

            return node.effectPercentPerRank * treeRank + masteryLevel * 0.5f;
        }
    }
}
