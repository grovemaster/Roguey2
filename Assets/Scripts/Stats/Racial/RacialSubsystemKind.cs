namespace JRogue.Stats.Racial
{
    /// <summary>
    /// Which race-specific progression/effect framework applies. One primary subsystem per character for v1;
    /// extend when multiple simultaneous frameworks are required.
    /// </summary>
    public enum RacialSubsystemKind : byte
    {
        None = 0,

        /// <summary>Barbarian Spirit Imprint graph (permanent branches).</summary>
        SpiritImprintBarbarian = 1,

        /// <summary>Human specialization path (optional class).</summary>
        HumanSpecialization = 2,

        /// <summary>Tiefling implant-style loadout with respec rules.</summary>
        TieflingImplants = 3,

        /// <summary>Elf elemental contracts / sustained spirits (design TBD).</summary>
        ElfElementalContracts = 4
    }
}
