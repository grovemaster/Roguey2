using JRogue.Stats;
using UnityEngine;

namespace JRogue.Racial
{
    public interface ISoulBeastLevelCapPolicy
    {
        int ResolveEffectiveCap(CharacterStats contractorStats, SoulBeastDefinition beastDef);
    }

    public sealed class CharacterLevelSoulBeastCapPolicy : ISoulBeastLevelCapPolicy
    {
        public static readonly CharacterLevelSoulBeastCapPolicy Instance = new CharacterLevelSoulBeastCapPolicy();

        public int ResolveEffectiveCap(CharacterStats contractorStats, SoulBeastDefinition beastDef)
        {
            if (beastDef == null)
                return 1;

            int spiritMax = Mathf.Max(1, beastDef.maxLevel);
            if (contractorStats == null)
                return spiritMax;

            int characterLevel = Mathf.Max(1, contractorStats.level);
            return Mathf.Clamp(characterLevel, 1, spiritMax);
        }
    }

    public sealed class DoubleCharacterLevelSoulBeastCapPolicy : ISoulBeastLevelCapPolicy
    {
        public static readonly DoubleCharacterLevelSoulBeastCapPolicy Instance = new DoubleCharacterLevelSoulBeastCapPolicy();

        public int ResolveEffectiveCap(CharacterStats contractorStats, SoulBeastDefinition beastDef)
        {
            if (beastDef == null)
                return 1;

            int spiritMax = Mathf.Max(1, beastDef.maxLevel);
            if (contractorStats == null)
                return spiritMax;

            int characterLevel = Mathf.Max(1, contractorStats.level);
            return Mathf.Clamp(characterLevel * 2, 1, spiritMax);
        }
    }
}
