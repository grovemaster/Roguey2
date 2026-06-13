using System.Collections.Generic;
using UnityEngine;

namespace JRogue.Racial
{
    [CreateAssetMenu(fileName = "ElementalSpiritLevelCurve", menuName = "JRogue/Racial/Elemental Spirit Level Curve")]
    public sealed class ElementalSpiritLevelCurve : ScriptableObject
    {
        [Tooltip("xpToReachNextLevel[i] = XP to advance from level (i+1) to level (i+2).")]
        public List<int> xpToReachNextLevel = new List<int> { 10, 20, 30 };

        public int GetXpRequiredForNextLevel(int currentContractLevel)
        {
            if (currentContractLevel < 1)
                return 0;

            int index = currentContractLevel - 1;
            if (xpToReachNextLevel == null || index >= xpToReachNextLevel.Count)
                return int.MaxValue;

            return Mathf.Max(0, xpToReachNextLevel[index]);
        }

        public int GetTotalXpForLevel(int targetLevel)
        {
            if (targetLevel <= 1)
                return 0;

            int total = 0;
            for (int level = 1; level < targetLevel; level++)
            {
                int step = GetXpRequiredForNextLevel(level);
                if (step == int.MaxValue)
                    break;
                total += step;
            }

            return total;
        }
    }
}
