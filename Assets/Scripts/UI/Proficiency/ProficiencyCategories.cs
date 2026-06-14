using System.Collections.Generic;
using JRogue.Stats;

namespace JRogue.UI.Proficiency
{
    public enum ProficiencyMenuCategory
    {
        GeneralCombat,
        Weapons,
        DamageTypes,
        Arcane,
        Divine,
        Other,
    }

    public enum ProficiencyCategoryFilter
    {
        All,
        Combat,
        Weapons,
        Damage,
        Magic,
        Divine,
        Other,
    }

    public static class ProficiencyCategories
    {
        static readonly ProficiencyKind[] GeneralCombat =
        {
            ProficiencyKind.Fighting,
            ProficiencyKind.Throwing,
            ProficiencyKind.Armour,
            ProficiencyKind.Dodging,
            ProficiencyKind.Shields,
        };

        static readonly ProficiencyKind[] Weapons =
        {
            ProficiencyKind.Weapon_Unarmed,
            ProficiencyKind.Weapon_Sword,
            ProficiencyKind.Weapon_Axe,
            ProficiencyKind.Weapon_Mace,
            ProficiencyKind.Weapon_Dagger,
            ProficiencyKind.Weapon_Bow,
            ProficiencyKind.Weapon_Staff,
            ProficiencyKind.Weapon_Polearm,
        };

        static readonly ProficiencyKind[] DamageTypes =
        {
            ProficiencyKind.Damage_Blunt,
            ProficiencyKind.Damage_Slash,
            ProficiencyKind.Damage_Pierce,
            ProficiencyKind.Damage_Fire,
            ProficiencyKind.Damage_Cold,
            ProficiencyKind.Damage_Lightning,
            ProficiencyKind.Damage_Poison,
            ProficiencyKind.Damage_Necrotic,
            ProficiencyKind.Damage_Radiant,
            ProficiencyKind.Damage_Acid,
            ProficiencyKind.Damage_Psychic,
            ProficiencyKind.Damage_Force,
        };

        static readonly ProficiencyKind[] Arcane =
        {
            ProficiencyKind.Spellcasting,
            ProficiencyKind.FireMagic,
            ProficiencyKind.IceMagic,
            ProficiencyKind.AirMagic,
            ProficiencyKind.EarthMagic,
            ProficiencyKind.Conjurations,
            ProficiencyKind.Hexes,
            ProficiencyKind.Translocations,
            ProficiencyKind.Alchemy,
        };

        static readonly ProficiencyKind[] Divine =
        {
            ProficiencyKind.DivineMagic,
            ProficiencyKind.Healing,
            ProficiencyKind.Smite,
            ProficiencyKind.Warding,
        };

        static readonly ProficiencyKind[] Other =
        {
            ProficiencyKind.DraconicSpellcraft,
            ProficiencyKind.Evocations,
            ProficiencyKind.Invocations,
        };

        public static ProficiencyMenuCategory GetCategory(ProficiencyKind kind) =>
            kind switch
            {
                ProficiencyKind.Fighting or ProficiencyKind.Throwing or ProficiencyKind.Armour
                    or ProficiencyKind.Dodging or ProficiencyKind.Shields => ProficiencyMenuCategory.GeneralCombat,
                ProficiencyKind.Weapon_Unarmed or ProficiencyKind.Weapon_Sword or ProficiencyKind.Weapon_Axe
                    or ProficiencyKind.Weapon_Mace or ProficiencyKind.Weapon_Dagger or ProficiencyKind.Weapon_Bow
                    or ProficiencyKind.Weapon_Staff or ProficiencyKind.Weapon_Polearm => ProficiencyMenuCategory.Weapons,
                ProficiencyKind.Damage_Blunt or ProficiencyKind.Damage_Slash or ProficiencyKind.Damage_Pierce
                    or ProficiencyKind.Damage_Fire or ProficiencyKind.Damage_Cold or ProficiencyKind.Damage_Lightning
                    or ProficiencyKind.Damage_Poison or ProficiencyKind.Damage_Necrotic or ProficiencyKind.Damage_Radiant
                    or ProficiencyKind.Damage_Acid or ProficiencyKind.Damage_Psychic or ProficiencyKind.Damage_Force
                    => ProficiencyMenuCategory.DamageTypes,
                ProficiencyKind.Spellcasting or ProficiencyKind.FireMagic or ProficiencyKind.IceMagic
                    or ProficiencyKind.AirMagic or ProficiencyKind.EarthMagic or ProficiencyKind.Conjurations
                    or ProficiencyKind.Hexes or ProficiencyKind.Translocations or ProficiencyKind.Alchemy
                    => ProficiencyMenuCategory.Arcane,
                ProficiencyKind.DivineMagic or ProficiencyKind.Healing or ProficiencyKind.Smite
                    or ProficiencyKind.Warding => ProficiencyMenuCategory.Divine,
                ProficiencyKind.DraconicSpellcraft or ProficiencyKind.Evocations or ProficiencyKind.Invocations
                    => ProficiencyMenuCategory.Other,
                _ => ProficiencyMenuCategory.Other,
            };

        public static string GetSectionHeader(ProficiencyMenuCategory category) =>
            category switch
            {
                ProficiencyMenuCategory.GeneralCombat => "GENERAL COMBAT",
                ProficiencyMenuCategory.Weapons => "WEAPONS",
                ProficiencyMenuCategory.DamageTypes => "DAMAGE TYPES",
                ProficiencyMenuCategory.Arcane => "ARCANE",
                ProficiencyMenuCategory.Divine => "DIVINE",
                ProficiencyMenuCategory.Other => "OTHER",
                _ => category.ToString().ToUpperInvariant(),
            };

        public static string GetFilterLabel(ProficiencyCategoryFilter filter) =>
            filter switch
            {
                ProficiencyCategoryFilter.All => "All",
                ProficiencyCategoryFilter.Combat => "Combat",
                ProficiencyCategoryFilter.Weapons => "Weapons",
                ProficiencyCategoryFilter.Damage => "Damage",
                ProficiencyCategoryFilter.Magic => "Magic",
                ProficiencyCategoryFilter.Divine => "Divine",
                ProficiencyCategoryFilter.Other => "Other",
                _ => filter.ToString(),
            };

        public static ProficiencyMenuCategory? ToMenuCategory(ProficiencyCategoryFilter filter) =>
            filter switch
            {
                ProficiencyCategoryFilter.Combat => ProficiencyMenuCategory.GeneralCombat,
                ProficiencyCategoryFilter.Weapons => ProficiencyMenuCategory.Weapons,
                ProficiencyCategoryFilter.Damage => ProficiencyMenuCategory.DamageTypes,
                ProficiencyCategoryFilter.Magic => ProficiencyMenuCategory.Arcane,
                ProficiencyCategoryFilter.Divine => ProficiencyMenuCategory.Divine,
                ProficiencyCategoryFilter.Other => ProficiencyMenuCategory.Other,
                _ => null,
            };

        public static IReadOnlyList<ProficiencyKind> GetKindsInCategory(ProficiencyMenuCategory category) =>
            category switch
            {
                ProficiencyMenuCategory.GeneralCombat => GeneralCombat,
                ProficiencyMenuCategory.Weapons => Weapons,
                ProficiencyMenuCategory.DamageTypes => DamageTypes,
                ProficiencyMenuCategory.Arcane => Arcane,
                ProficiencyMenuCategory.Divine => Divine,
                ProficiencyMenuCategory.Other => Other,
                _ => GeneralCombat,
            };

        public static IReadOnlyList<ProficiencyMenuCategory> GetAllSections()
        {
            return new[]
            {
                ProficiencyMenuCategory.GeneralCombat,
                ProficiencyMenuCategory.Weapons,
                ProficiencyMenuCategory.DamageTypes,
                ProficiencyMenuCategory.Arcane,
                ProficiencyMenuCategory.Divine,
                ProficiencyMenuCategory.Other,
            };
        }

        public static IReadOnlyList<ProficiencyCategoryFilter> GetAllFilters()
        {
            return new[]
            {
                ProficiencyCategoryFilter.All,
                ProficiencyCategoryFilter.Combat,
                ProficiencyCategoryFilter.Weapons,
                ProficiencyCategoryFilter.Damage,
                ProficiencyCategoryFilter.Magic,
                ProficiencyCategoryFilter.Divine,
                ProficiencyCategoryFilter.Other,
            };
        }
    }
}
