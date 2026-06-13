using JRogue.Stats;
using UnityEngine;

namespace JRogue.Racial
{
    public static class SoulBeastProgressionLogic
    {
        static ISoulBeastLevelCapPolicy _capPolicy = CharacterLevelSoulBeastCapPolicy.Instance;

        public static ISoulBeastLevelCapPolicy CapPolicy
        {
            get => _capPolicy ?? CharacterLevelSoulBeastCapPolicy.Instance;
            set => _capPolicy = value;
        }

        public static int GetEffectiveLevelCap(CharacterStats contractorStats, SoulBeastDefinition beastDef) =>
            CapPolicy.ResolveEffectiveCap(contractorStats, beastDef);

        public static bool IsAtCap(BeastmanSoulBeastRuntime runtime)
        {
            if (runtime == null || !runtime.IsBonded)
                return true;

            CharacterStats stats = runtime.GetComponent<CharacterStats>();
            if (stats == null || !runtime.TryResolveBondedDefinition(out SoulBeastDefinition beast))
                return true;

            return runtime.SoulBeastLevel >= GetEffectiveLevelCap(stats, beast);
        }
    }
}
