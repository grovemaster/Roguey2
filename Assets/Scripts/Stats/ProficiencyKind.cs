namespace JRogue.Stats
{
    /// <summary>Trainable proficiency id. See Docs/Progression/Proficiencies-Requirements.md.</summary>
    public enum ProficiencyKind
    {
        None = 0,

        Fighting,
        Throwing,
        Armour,
        Dodging,
        Shields,

        Weapon_Unarmed,
        Weapon_Sword,
        Weapon_Axe,
        Weapon_Mace,
        Weapon_Dagger,
        Weapon_Bow,
        Weapon_Staff,
        Weapon_Polearm,

        Damage_Blunt,
        Damage_Slash,
        Damage_Pierce,
        Damage_Fire,
        Damage_Cold,
        Damage_Lightning,
        Damage_Poison,
        Damage_Necrotic,
        Damage_Radiant,
        Damage_Acid,
        Damage_Psychic,
        Damage_Force,

        Spellcasting,
        FireMagic,
        IceMagic,
        AirMagic,
        EarthMagic,
        Conjurations,
        Hexes,
        Translocations,
        Alchemy,

        DivineMagic,
        Healing,
        Smite,
        Warding,

        DraconicSpellcraft,
        Evocations,
        Invocations,
    }
}
