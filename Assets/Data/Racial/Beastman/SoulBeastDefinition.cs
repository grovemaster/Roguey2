using System.Collections.Generic;
using UnityEngine;

namespace JRogue.Racial
{
    [CreateAssetMenu(fileName = "SoulBeast", menuName = "JRogue/Racial/Soul Beast")]
    public sealed class SoulBeastDefinition : ScriptableObject
    {
        public string soulBeastId;
        public string displayName;
        [TextArea] public string description;
        public SoulBeastType soulBeastType;

        [Min(1)] public int maxLevel = 1;

        [Tooltip("Optional tags for ritual offering filters.")]
        public List<string> tags = new List<string>();

        public List<SoulBeastLevelData> levels = new List<SoulBeastLevelData>();

        public bool TryGetLevelRow(int level, out SoulBeastLevelData row)
        {
            row = null;
            if (level < 1 || levels == null || level > levels.Count)
                return false;

            row = levels[level - 1];
            return row != null;
        }

        public bool HasTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag) || tags == null)
                return false;

            string needle = tag.Trim();
            foreach (string candidate in tags)
            {
                if (candidate != null && candidate.Trim() == needle)
                    return true;
            }

            return false;
        }
    }
}
