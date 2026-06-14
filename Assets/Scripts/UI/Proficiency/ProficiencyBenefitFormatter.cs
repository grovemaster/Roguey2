using JRogue.Stats;
using UnityEngine;

namespace JRogue.UI.Proficiency
{
    public static class ProficiencyBenefitFormatter
    {
        public static string GetSummary(ProficiencyKind kind, int level)
        {
            if (level <= 0)
                return "Untrained — no bonus yet.";

            return kind switch
            {
                ProficiencyKind.Fighting =>
                    $"+{Percent(level, 30f)}% melee and ranged damage (Fighting modifier).",
                ProficiencyKind.Throwing =>
                    $"+{Percent(level, 25f)}% thrown weapon damage; improved range at higher levels.",
                ProficiencyKind.Armour =>
                    "Armour training reduces penalties and improves mitigation at higher levels.",
                ProficiencyKind.Dodging =>
                    $"+{Mathf.Max(0, level / 4)} effective Acrobatics on trap dodges.",
                ProficiencyKind.Shields =>
                    $"{Mathf.Min(40, level * 2)}% block chance when a shield is equipped.",
                ProficiencyKind.Weapon_Bow =>
                    $"+{Percent(level, 25f)}% bow damage; +{level / 3} accuracy (future).",
                ProficiencyKind.Weapon_Unarmed or ProficiencyKind.Weapon_Sword or ProficiencyKind.Weapon_Axe
                    or ProficiencyKind.Weapon_Mace or ProficiencyKind.Weapon_Dagger or ProficiencyKind.Weapon_Staff
                    or ProficiencyKind.Weapon_Polearm =>
                    $"+{Percent(level, 25f)}% weapon damage; +{level / 3} accuracy (future).",
                ProficiencyKind.Damage_Blunt or ProficiencyKind.Damage_Slash or ProficiencyKind.Damage_Pierce
                    or ProficiencyKind.Damage_Fire or ProficiencyKind.Damage_Cold or ProficiencyKind.Damage_Lightning
                    or ProficiencyKind.Damage_Poison or ProficiencyKind.Damage_Necrotic or ProficiencyKind.Damage_Radiant
                    or ProficiencyKind.Damage_Acid or ProficiencyKind.Damage_Psychic or ProficiencyKind.Damage_Force =>
                    $"+{Percent(level, 35f)}% damage when this damage type is primary on a hit.",
                ProficiencyKind.Spellcasting =>
                    $"+{level * 3}% spell power on eligible spells.",
                ProficiencyKind.FireMagic or ProficiencyKind.IceMagic or ProficiencyKind.AirMagic
                    or ProficiencyKind.EarthMagic or ProficiencyKind.Conjurations or ProficiencyKind.Hexes
                    or ProficiencyKind.Translocations or ProficiencyKind.Alchemy =>
                    $"+{level * 4}% power on {ProficiencyDisplayNames.Get(kind)} spells.",
                ProficiencyKind.DivineMagic or ProficiencyKind.Healing or ProficiencyKind.Smite
                    or ProficiencyKind.Warding =>
                    $"+{level * 4}% power on {ProficiencyDisplayNames.Get(kind)} effects.",
                ProficiencyKind.DraconicSpellcraft =>
                    $"+{level * 3}% power on Dragonian spells.",
                ProficiencyKind.Evocations =>
                    $"+{Mathf.FloorToInt(level / 3f) * 5}% evocable item potency.",
                ProficiencyKind.Invocations =>
                    "Invocations are not trainable in v0.",
                _ => "Higher levels improve related combat and utility outcomes.",
            };
        }

        static int Percent(int level, float divisor) =>
            Mathf.RoundToInt((level / divisor) * 100f);
    }
}
