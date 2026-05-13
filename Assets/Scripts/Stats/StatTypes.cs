namespace JRogue.Stats
{
    /***************************** VIOLENCE *******************************/
    public enum DamageType { Blunt, Slash, Pierce, Fire, Cold, Necrotic, Radiant, Poison, Acid, Psychic, Lightning, Force }
    public enum WeaponType { Unarmed, Sword, Axe, Mace, Dagger, Bow, Staff, Polearm }
    public enum SkillType { Stealth, Athletics, Acrobatics, Perception, Insight, Survival, Medicine }

    /****************************** RACES *********************************/
    /// <summary>Ancestry / people. Explicit numeric values for stable saves and networking.</summary>
    public enum Race : byte
    {
        Unset = 0,
        Human = 1,
        Elf = 2,
        Barbarian = 3,
        Dwarf = 4,
        Beastman = 5,
        Dragonian = 6,
        Tiefling = 7,
        Undead = 8,
        Fairy = 9
    }
    public enum Gender { None, Male, Female, Other }
    public enum Alignment { LawfulGood, NeutralGood, ChaoticGood, LawfulNeutral, TrueNeutral, ChaoticNeutral, LawfulEvil, NeutralEvil, ChaoticEvil }

    /****************************** STATS *********************************/
    public enum StatType
    {
        Strength, Dexterity, Agility, Constitution,
        Intelligence, Wisdom, Charisma, Luck,
        Sight, Hearing, Smell
    }
}