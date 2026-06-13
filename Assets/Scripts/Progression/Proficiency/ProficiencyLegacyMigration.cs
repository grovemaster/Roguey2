using JRogue.Stats;
using UnityEngine;

namespace JRogue.Progression.Proficiency
{
    public static class ProficiencyLegacyMigration
    {
        public static void MigrateFromWeaponProficiencies(CharacterStats stats, ProficiencyRuntime runtime)
        {
            if (stats == null || runtime == null || stats.WeaponProficiencies == null)
                return;

            foreach (var pair in stats.WeaponProficiencies)
            {
                int legacy = pair.Value != null ? pair.Value.GetValue() : 0;
                if (legacy <= 0)
                    continue;

                ProficiencyKind kind = ProficiencyKindMapping.FromWeaponType(pair.Key);
                if (kind == ProficiencyKind.None)
                    continue;

                if (runtime.GetLevel(kind) < legacy)
                    runtime.SetLevelForTests(kind, Mathf.Min(legacy, ProficiencyRules.MaxLevel));
            }
        }
    }
}
