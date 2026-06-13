using JRogue.Stats;
using UnityEngine;

namespace JRogue.Racial
{
    public interface IElementalSpiritLevelCapPolicy
    {
        int ResolveEffectiveCap(
            CharacterStats elfStats,
            ElementalSpiritContractPreset instance,
            ElementalSpiritDefinition spiritDef);
    }

    public sealed class CharacterLevelSpiritCapPolicy : IElementalSpiritLevelCapPolicy
    {
        public static readonly CharacterLevelSpiritCapPolicy Instance = new CharacterLevelSpiritCapPolicy();

        public int ResolveEffectiveCap(
            CharacterStats elfStats,
            ElementalSpiritContractPreset instance,
            ElementalSpiritDefinition spiritDef)
        {
            if (spiritDef == null)
                return 1;

            int spiritMax = Mathf.Max(1, spiritDef.maxLevel);
            if (elfStats == null)
                return spiritMax;

            int characterLevel = Mathf.Max(1, elfStats.level);
            return Mathf.Clamp(characterLevel, 1, spiritMax);
        }
    }
}
