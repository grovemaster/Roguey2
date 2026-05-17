using System.Collections.Generic;
using UnityEngine;

namespace JRogue.Racial
{
    [CreateAssetMenu(fileName = "ElementalSpirit", menuName = "JRogue/Racial/Elemental Spirit")]
    public class ElementalSpiritDefinition : ScriptableObject
    {
        [Tooltip("Stable id for saves and summon rules.")]
        public string spiritId;

        public string displayName;
        [TextArea] public string description;
        public ElementalElement element;

        [Min(1)] public int maxLevel = 1;
        [Min(0)] public int summonSoulPowerCost = 1;
        [Min(0)] public int upkeepSoulPowerPerTurn = 1;

        public List<ElementalSpiritLevelData> levels = new List<ElementalSpiritLevelData>();

        public bool TryGetLevelRow(int level, out ElementalSpiritLevelData row)
        {
            row = null;
            if (level < 1 || levels == null || level > levels.Count)
                return false;
            row = levels[level - 1];
            return row != null;
        }
    }
}
