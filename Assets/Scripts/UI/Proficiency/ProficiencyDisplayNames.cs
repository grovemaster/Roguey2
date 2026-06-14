using JRogue.Stats;

namespace JRogue.UI.Proficiency
{
    public static class ProficiencyDisplayNames
    {
        public static string Get(ProficiencyKind kind) =>
            kind switch
            {
                ProficiencyKind.None => string.Empty,
                ProficiencyKind.Fighting => "Fighting",
                ProficiencyKind.Throwing => "Throwing",
                ProficiencyKind.Armour => "Armour",
                ProficiencyKind.Dodging => "Dodging",
                ProficiencyKind.Shields => "Shields",
                ProficiencyKind.Weapon_Unarmed => "Unarmed",
                ProficiencyKind.Weapon_Sword => "Sword",
                ProficiencyKind.Weapon_Axe => "Axe",
                ProficiencyKind.Weapon_Mace => "Mace",
                ProficiencyKind.Weapon_Dagger => "Dagger",
                ProficiencyKind.Weapon_Bow => "Bow",
                ProficiencyKind.Weapon_Staff => "Staff",
                ProficiencyKind.Weapon_Polearm => "Polearm",
                ProficiencyKind.Damage_Blunt => "Blunt damage",
                ProficiencyKind.Damage_Slash => "Slash damage",
                ProficiencyKind.Damage_Pierce => "Pierce damage",
                ProficiencyKind.Damage_Fire => "Fire damage",
                ProficiencyKind.Damage_Cold => "Cold damage",
                ProficiencyKind.Damage_Lightning => "Lightning damage",
                ProficiencyKind.Damage_Poison => "Poison damage",
                ProficiencyKind.Damage_Necrotic => "Necrotic damage",
                ProficiencyKind.Damage_Radiant => "Radiant damage",
                ProficiencyKind.Damage_Acid => "Acid damage",
                ProficiencyKind.Damage_Psychic => "Psychic damage",
                ProficiencyKind.Damage_Force => "Force damage",
                ProficiencyKind.Spellcasting => "Spellcasting",
                ProficiencyKind.FireMagic => "Fire Magic",
                ProficiencyKind.IceMagic => "Ice Magic",
                ProficiencyKind.AirMagic => "Air Magic",
                ProficiencyKind.EarthMagic => "Earth Magic",
                ProficiencyKind.Conjurations => "Conjurations",
                ProficiencyKind.Hexes => "Hexes",
                ProficiencyKind.Translocations => "Translocations",
                ProficiencyKind.Alchemy => "Alchemy",
                ProficiencyKind.DivineMagic => "Divine Magic",
                ProficiencyKind.Healing => "Healing",
                ProficiencyKind.Smite => "Smite",
                ProficiencyKind.Warding => "Warding",
                ProficiencyKind.DraconicSpellcraft => "Draconic Spellcraft",
                ProficiencyKind.Evocations => "Evocations",
                ProficiencyKind.Invocations => "Invocations",
                _ => kind.ToString(),
            };
    }
}
