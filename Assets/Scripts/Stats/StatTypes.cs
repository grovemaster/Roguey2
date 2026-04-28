namespace JRogue.Stats
{
    /***************************** VIOLENCE *******************************/
    public enum DamageType { Blunt, Slash, Pierce, Fire, Cold, Necrotic, Radiant, Poison, Acid, Psychic, Lightning, Force }
    public enum WeaponType { Unarmed, Sword, Axe, Mace, Dagger, Bow, Staff, Polearm }
    public enum SkillType { Stealth, Athletics, Acrobatics, Perception, Insight, Survival, Medicine }

    /****************************** RACES *********************************/
    public enum Race { Human, Elf, Dwarf, Orc, Undead, Construct, Beast }
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