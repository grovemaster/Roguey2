using JRogue.Stats;

namespace JRogue.UI.Proficiency
{
    public static class ProficiencyTrainedByFormatter
    {
        public static string GetHint(ProficiencyKind kind) =>
            kind switch
            {
                ProficiencyKind.Fighting =>
                    "Successful weapon hits (secondary award at 50% pxp).",
                ProficiencyKind.Throwing => "Successful thrown weapon or item attacks.",
                ProficiencyKind.Armour => "Taking damage while wearing torso, legs, or head armour.",
                ProficiencyKind.Dodging => "Successfully avoiding traps (and future evasion hooks).",
                ProficiencyKind.Shields => "Successful blocks with an off-hand shield.",
                ProficiencyKind.Weapon_Unarmed => "Successful unarmed hits.",
                ProficiencyKind.Weapon_Sword or ProficiencyKind.Weapon_Axe or ProficiencyKind.Weapon_Mace
                    or ProficiencyKind.Weapon_Dagger or ProficiencyKind.Weapon_Bow or ProficiencyKind.Weapon_Staff
                    or ProficiencyKind.Weapon_Polearm =>
                    $"Successful hits with {ProficiencyDisplayNames.Get(kind).ToLowerInvariant()} weapons.",
                ProficiencyKind.Damage_Blunt or ProficiencyKind.Damage_Slash or ProficiencyKind.Damage_Pierce
                    or ProficiencyKind.Damage_Fire or ProficiencyKind.Damage_Cold or ProficiencyKind.Damage_Lightning
                    or ProficiencyKind.Damage_Poison or ProficiencyKind.Damage_Necrotic or ProficiencyKind.Damage_Radiant
                    or ProficiencyKind.Damage_Acid or ProficiencyKind.Damage_Psychic or ProficiencyKind.Damage_Force =>
                    "Active damage modules on successful hits or spell casts (spells at 50% pxp).",
                ProficiencyKind.Spellcasting or ProficiencyKind.FireMagic or ProficiencyKind.IceMagic
                    or ProficiencyKind.AirMagic or ProficiencyKind.EarthMagic or ProficiencyKind.Conjurations
                    or ProficiencyKind.Hexes or ProficiencyKind.Translocations or ProficiencyKind.Alchemy =>
                    "Successful Human Mage spell casts tagged with this school.",
                ProficiencyKind.DivineMagic or ProficiencyKind.Healing or ProficiencyKind.Smite
                    or ProficiencyKind.Warding =>
                    "Successful Human Priest abilities tagged with this proficiency.",
                ProficiencyKind.DraconicSpellcraft =>
                    "Successful Dragonian spell casts.",
                ProficiencyKind.Evocations => "Using evocable items and wands.",
                ProficiencyKind.Invocations => "Not trainable in v0.",
                _ => "Practice through qualifying actions in the field.",
            };
    }
}
