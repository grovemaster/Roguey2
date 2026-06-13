using UnityEngine;

namespace JRogue.Progression.Proficiency
{
    public static class ProficiencyRules
    {
        public const int MaxLevel = 27;
        public const float FightingSecondaryFraction = 0.5f;
        public const float SpellDamageTypeFraction = 0.5f;
        public const int MaxAwardsPerAction = 12;

        public static int GetTrainingCap(int characterLevel) =>
            Mathf.Min(MaxLevel, 2 * Mathf.Max(1, characterLevel));

        public static int GetBaseXpToNextLevel(int currentLevel)
        {
            int next = currentLevel + 1;
            return (next * next * 10) + (next * 4);
        }

        public static int GetXpToNextLevel(int currentLevel, int aptitude)
        {
            float multiplier = ProficiencyAptitudeService.GetXpMultiplier(aptitude);
            return Mathf.Max(1, Mathf.FloorToInt(GetBaseXpToNextLevel(currentLevel) * multiplier));
        }

        public static float GetAptitudeXpMultiplier(int aptitude) =>
            ProficiencyAptitudeService.GetXpMultiplier(aptitude);
    }
}
