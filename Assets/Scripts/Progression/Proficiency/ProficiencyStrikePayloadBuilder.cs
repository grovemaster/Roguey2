using System.Collections.Generic;
using JRogue.Ability;
using JRogue.Actors;
using JRogue.Item;
using JRogue.Racial;
using JRogue.Stats;
using UnityEngine;

namespace JRogue.Progression.Proficiency
{
    public enum ProficiencyActionTier
    {
        StandardMeleeOrRangedHit,
        HeavyHit,
        SpellCast,
        CheapCantrip,
        ArmourTick,
        TrapDodge,
    }

    public sealed class ProficiencyResolvedAction
    {
        public WeaponType WeaponType = WeaponType.Unarmed;
        public bool HasWeaponType;
        public List<DamageEntry> DamageModulesApplied = new();
        public List<ProficiencyKind> ProficiencyTags = new();
        public bool CountsAsWeaponHit;
        public bool SpellDamageTypesAtHalfRate;
        public int ProficiencyXpOverride;
        public ProficiencyActionTier Tier = ProficiencyActionTier.StandardMeleeOrRangedHit;
        public int BaseDamageForTier;
    }

    public static class ProficiencyStrikePayloadBuilder
    {
        public static ProficiencyResolvedAction FromMeleeWeapon(BaseActor actor, ItemData weapon, int totalBaseDamage)
        {
            var action = new ProficiencyResolvedAction
            {
                HasWeaponType = true,
                WeaponType = weapon != null ? weapon.weaponType : WeaponType.Unarmed,
                CountsAsWeaponHit = true,
                Tier = ResolveMeleeTier(totalBaseDamage),
                BaseDamageForTier = totalBaseDamage,
            };

            if (weapon == null || weapon.weaponType == WeaponType.Unarmed)
            {
                action.WeaponType = WeaponType.Unarmed;
                action.DamageModulesApplied.Add(new DamageEntry { type = DamageType.Blunt, value = 1 });
            }
            else
            {
                AppendItemModules(action.DamageModulesApplied, weapon);
            }

            AppendFireImbue(actor, action.DamageModulesApplied);
            return action;
        }

        public static ProficiencyResolvedAction FromUnarmed(int baseDamage)
        {
            return new ProficiencyResolvedAction
            {
                HasWeaponType = true,
                WeaponType = WeaponType.Unarmed,
                CountsAsWeaponHit = true,
                Tier = ResolveMeleeTier(baseDamage),
                BaseDamageForTier = baseDamage,
                DamageModulesApplied = new List<DamageEntry>
                {
                    new() { type = DamageType.Blunt, value = Mathf.Max(1, baseDamage) },
                },
            };
        }

        public static ProficiencyResolvedAction FromBowShot(ItemData bow, ItemData arrow, int totalDamage)
        {
            var action = new ProficiencyResolvedAction
            {
                HasWeaponType = true,
                WeaponType = WeaponType.Bow,
                CountsAsWeaponHit = true,
                Tier = ResolveMeleeTier(totalDamage),
                BaseDamageForTier = totalDamage,
            };

            AppendItemModules(action.DamageModulesApplied, bow);
            AppendItemModules(action.DamageModulesApplied, arrow);
            return action;
        }

        public static ProficiencyResolvedAction FromMageSpellCast(MageSpellDefinition spell, AbilityAction ability)
        {
            var action = new ProficiencyResolvedAction
            {
                Tier = ResolveSpellTier(spell, ability),
                SpellDamageTypesAtHalfRate = true,
            };

            foreach (ProficiencyKind tag in ProficiencyMageSpellTags.Resolve(spell, ability))
                action.ProficiencyTags.Add(tag);

            AppendSpellTrainingModules(action.DamageModulesApplied, action.ProficiencyTags);
            return action;
        }

        public static ProficiencyResolvedAction FromDragonianSpellCast(
            DragonianSpellDefinition spell,
            AbilityAction ability)
        {
            var action = new ProficiencyResolvedAction
            {
                Tier = ResolveDragonianSpellTier(spell, ability),
            };

            action.ProficiencyTags.Add(ProficiencyKind.DraconicSpellcraft);
            if (ability?.proficiencyTags != null)
            {
                for (int i = 0; i < ability.proficiencyTags.Count; i++)
                {
                    ProficiencyKind tag = ability.proficiencyTags[i];
                    if (tag != ProficiencyKind.None && !action.ProficiencyTags.Contains(tag))
                        action.ProficiencyTags.Add(tag);
                }
            }

            return action;
        }

        public static ProficiencyResolvedAction FromAbility(AbilityAction ability, bool countsAsWeaponHit = false)
        {
            var action = new ProficiencyResolvedAction
            {
                CountsAsWeaponHit = countsAsWeaponHit,
                ProficiencyXpOverride = ability != null ? ability.proficiencyXpOverride : 0,
                Tier = ResolveAbilityTier(ability),
            };

            if (ability?.proficiencyTags != null)
            {
                for (int i = 0; i < ability.proficiencyTags.Count; i++)
                {
                    if (ability.proficiencyTags[i] != ProficiencyKind.None)
                        action.ProficiencyTags.Add(ability.proficiencyTags[i]);
                }
            }

            return action;
        }

        static void AppendFireImbue(BaseActor actor, List<DamageEntry> modules)
        {
            if (actor == null)
                return;

            ElementalSpiritContractsRuntime spirits = actor.GetComponent<ElementalSpiritContractsRuntime>();
            if (spirits == null || spirits.WeaponFireImbueBonus <= 0)
                return;

            modules.Add(new DamageEntry
            {
                type = DamageType.Fire,
                value = spirits.WeaponFireImbueBonus,
            });
        }

        static void AppendItemModules(List<DamageEntry> modules, ItemData item)
        {
            if (item?.damageModules == null)
                return;

            for (int i = 0; i < item.damageModules.Count; i++)
            {
                DamageEntry module = item.damageModules[i];
                if (module.value > 0)
                    modules.Add(module);
            }
        }

        static void AppendSpellTrainingModules(List<DamageEntry> modules, List<ProficiencyKind> tags)
        {
            for (int i = 0; i < tags.Count; i++)
            {
                DamageType? damageType = ProficiencyMageSpellTags.DamageTypeForSchoolTag(tags[i]);
                if (!damageType.HasValue)
                    continue;

                modules.Add(new DamageEntry { type = damageType.Value, value = 1 });
            }
        }

        static ProficiencyActionTier ResolveMeleeTier(int totalBaseDamage) =>
            totalBaseDamage >= 20
                ? ProficiencyActionTier.HeavyHit
                : ProficiencyActionTier.StandardMeleeOrRangedHit;

        static ProficiencyActionTier ResolveSpellTier(MageSpellDefinition spell, AbilityAction ability)
        {
            int cost = spell != null ? spell.magicPowerCost : 0;
            if (ability != null && ability.magicPowerCost > cost)
                cost = ability.magicPowerCost;

            return cost <= 2
                ? ProficiencyActionTier.CheapCantrip
                : ProficiencyActionTier.SpellCast;
        }

        static ProficiencyActionTier ResolveDragonianSpellTier(
            DragonianSpellDefinition spell,
            AbilityAction ability)
        {
            int cost = spell != null ? spell.soulPowerCastCost : 0;
            if (ability != null && ability.soulPowerCost > cost)
                cost = ability.soulPowerCost;

            return cost <= 2
                ? ProficiencyActionTier.CheapCantrip
                : ProficiencyActionTier.SpellCast;
        }

        static ProficiencyActionTier ResolveAbilityTier(AbilityAction ability)
        {
            if (ability == null)
                return ProficiencyActionTier.StandardMeleeOrRangedHit;

            int cost = Mathf.Max(ability.magicPowerCost, ability.soulPowerCost, ability.divinePowerCost);
            return cost <= 2 && cost > 0
                ? ProficiencyActionTier.CheapCantrip
                : ProficiencyActionTier.StandardMeleeOrRangedHit;
        }
    }
}
