using JRogue.Stats;

namespace JRogue.Progression.Proficiency
{
    public static class ProficiencyKindMapping
    {
        public static ProficiencyKind FromWeaponType(WeaponType weaponType) =>
            weaponType switch
            {
                WeaponType.Unarmed => ProficiencyKind.Weapon_Unarmed,
                WeaponType.Sword => ProficiencyKind.Weapon_Sword,
                WeaponType.Axe => ProficiencyKind.Weapon_Axe,
                WeaponType.Mace => ProficiencyKind.Weapon_Mace,
                WeaponType.Dagger => ProficiencyKind.Weapon_Dagger,
                WeaponType.Bow => ProficiencyKind.Weapon_Bow,
                WeaponType.Staff => ProficiencyKind.Weapon_Staff,
                WeaponType.Polearm => ProficiencyKind.Weapon_Polearm,
                _ => ProficiencyKind.None,
            };

        public static ProficiencyKind FromDamageType(DamageType damageType) =>
            damageType switch
            {
                DamageType.Blunt => ProficiencyKind.Damage_Blunt,
                DamageType.Slash => ProficiencyKind.Damage_Slash,
                DamageType.Pierce => ProficiencyKind.Damage_Pierce,
                DamageType.Fire => ProficiencyKind.Damage_Fire,
                DamageType.Cold => ProficiencyKind.Damage_Cold,
                DamageType.Lightning => ProficiencyKind.Damage_Lightning,
                DamageType.Poison => ProficiencyKind.Damage_Poison,
                DamageType.Necrotic => ProficiencyKind.Damage_Necrotic,
                DamageType.Radiant => ProficiencyKind.Damage_Radiant,
                DamageType.Acid => ProficiencyKind.Damage_Acid,
                DamageType.Psychic => ProficiencyKind.Damage_Psychic,
                DamageType.Force => ProficiencyKind.Damage_Force,
                _ => ProficiencyKind.None,
            };

        public static bool IsArcaneSchool(ProficiencyKind kind) =>
            kind is ProficiencyKind.FireMagic
                or ProficiencyKind.IceMagic
                or ProficiencyKind.AirMagic
                or ProficiencyKind.EarthMagic
                or ProficiencyKind.Conjurations
                or ProficiencyKind.Hexes
                or ProficiencyKind.Translocations
                or ProficiencyKind.Alchemy
                or ProficiencyKind.Spellcasting;

        public static bool IsDivineSchool(ProficiencyKind kind) =>
            kind is ProficiencyKind.DivineMagic
                or ProficiencyKind.Healing
                or ProficiencyKind.Smite
                or ProficiencyKind.Warding;
    }
}
