using System.Collections.Generic;
using JRogue.Actors;
using JRogue.Item;
using JRogue.Stats;
using UnityEngine;

namespace JRogue.Progression.Proficiency
{
    public static class ProficiencyCombatResolver
    {
        public static int ComputeBowShotDamage(BaseActor actor, ItemData bow, ItemData arrow)
        {
            if (actor == null || bow == null || arrow == null)
                return 1;

            int baseDamage = SumDamage(bow) + SumDamage(arrow);
            return ComputePhysicalDamage(
                actor,
                baseDamage,
                WeaponType.Bow,
                CollectModules(bow, arrow));
        }

        public static int ComputePhysicalDamage(
            BaseActor actor,
            int baseDamage,
            WeaponType weaponType,
            IReadOnlyList<DamageEntry> damageModules)
        {
            if (baseDamage <= 0)
                baseDamage = 1;

            CharacterStats stats = actor != null ? actor.GetComponent<CharacterStats>() : null;
            if (weaponType == WeaponType.Bow)
            {
                int dex = stats != null ? stats.Dexterity.GetValue() : 10;
                baseDamage = AttackDamageLogic.ApplyRangedDexterityBonus(baseDamage, dex);
            }
            else
            {
                int strength = stats != null ? stats.Strength.GetValue() : 10;
                baseDamage = AttackDamageLogic.ApplyMeleeStrengthBonus(baseDamage, strength);
            }

            ProficiencyRuntime runtime = actor != null
                ? actor.GetComponent<ProficiencyRuntime>()
                : null;

            if (runtime == null)
                return baseDamage;

            int weaponLevel = runtime.GetLevel(ProficiencyKindMapping.FromWeaponType(weaponType));
            int fightingLevel = runtime.GetLevel(ProficiencyKind.Fighting);
            int damageTypeLevel = 0;

            if (damageModules != null && damageModules.Count > 0)
            {
                ProficiencyKind damageKind =
                    ProficiencyKindMapping.FromDamageType(damageModules[0].type);
                damageTypeLevel = runtime.GetLevel(damageKind);
            }

            float weaponMod = 1f + weaponLevel / 25f;
            float fightingMod = 1f + fightingLevel / 30f;
            float damageMod = 1f + damageTypeLevel / 35f;
            return Mathf.Max(1, Mathf.RoundToInt(baseDamage * weaponMod * fightingMod * damageMod));
        }

        public static float GetSpellPowerMultiplier(BaseActor actor, IReadOnlyList<ProficiencyKind> tags)
        {
            ProficiencyRuntime runtime = actor?.GetComponent<ProficiencyRuntime>();
            if (runtime == null)
                return 1f;

            float spellcastingLevel = runtime.GetLevel(ProficiencyKind.Spellcasting);
            float multiplier = 1f + spellcastingLevel * 0.03f;

            if (tags == null)
                return multiplier;

            for (int i = 0; i < tags.Count; i++)
            {
                if (ProficiencyKindMapping.IsArcaneSchool(tags[i]) && tags[i] != ProficiencyKind.Spellcasting)
                    multiplier *= 1f + runtime.GetLevel(tags[i]) * 0.04f;
            }

            return multiplier;
        }

        static int SumDamage(ItemData item)
        {
            if (item?.damageModules == null)
                return 0;

            int total = 0;
            for (int i = 0; i < item.damageModules.Count; i++)
                total += item.damageModules[i].value;

            return total;
        }

        static List<DamageEntry> CollectModules(ItemData bow, ItemData arrow)
        {
            var modules = new List<DamageEntry>();
            AppendModules(modules, bow);
            AppendModules(modules, arrow);
            return modules;
        }

        static void AppendModules(List<DamageEntry> modules, ItemData item)
        {
            if (item?.damageModules == null)
                return;

            for (int i = 0; i < item.damageModules.Count; i++)
            {
                if (item.damageModules[i].value > 0)
                    modules.Add(item.damageModules[i]);
            }
        }
    }
}
