using UnityEngine;

namespace JRogue.Data.Progression
{
    [CreateAssetMenu(fileName = "ExperienceCurve", menuName = "JRogue/Progression/Experience Curve")]
    public class ExperienceCurve : ScriptableObject
    {
        public const int DefaultMaxLevel = 50;

        [Tooltip("XP required to advance from level N to N+1 = baseXpPerLevel × N.")]
        [Min(1)]
        public int baseXpPerLevel = 100;

        [Min(1)]
        public int constitutionPerLevel = 1;

        [Min(0)]
        public int maxSoulPowerPerLevel = 2;

        public int MaxLevel => DefaultMaxLevel;

        public int GetXpRequiredForNextLevel(int currentLevel)
        {
            if (currentLevel < 1 || currentLevel >= DefaultMaxLevel)
                return int.MaxValue;
            return baseXpPerLevel * currentLevel;
        }
    }
}
