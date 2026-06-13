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
        ElfElementalContracts = 4,

        /// <summary>Dwarf patron Ancestor path + common racial ability slots.</summary>
        DwarfAncestry = 5,

        /// <summary>Undead Diablo-style skill tree (respec allowed).</summary>
        UndeadSkillTree = 6,

        /// <summary>Beastman Soul Beast bond and linear ability chain (permanent).</summary>
        BeastmanSoulBeast = 7,

        /// <summary>Dragonian spell library with Soul Power memory budget (permanent learn, flexible memorize).</summary>
        DragonianSpells = 8
    }

    /// <summary>
    /// Maps <see cref="RacialSubsystemKind"/> to Phase 0 commitment policy and folk eligibility hints.
    /// </summary>
    public static class RacialSubsystemCatalog
    {
        public static RacialCommitmentPolicy GetCommitmentPolicy(RacialSubsystemKind kind)
        {
            switch (kind)
            {
                case RacialSubsystemKind.SpiritImprintBarbarian:
                case RacialSubsystemKind.HumanSpecialization:
                case RacialSubsystemKind.ElfElementalContracts:
                case RacialSubsystemKind.DwarfAncestry:
                case RacialSubsystemKind.BeastmanSoulBeast:
                case RacialSubsystemKind.DragonianSpells:
                    return RacialCommitmentPolicy.Permanent;
                case RacialSubsystemKind.TieflingImplants:
                case RacialSubsystemKind.UndeadSkillTree:
                    return RacialCommitmentPolicy.RespecAllowed;
                default:
                    return RacialCommitmentPolicy.NotApplicable;
            }
        }

        public static bool IsSubsystemValidForRace(RacialSubsystemKind subsystem, Race race)
        {
            if (subsystem == RacialSubsystemKind.None)
                return true;

            switch (subsystem)
            {
                case RacialSubsystemKind.SpiritImprintBarbarian:
                    return race == Race.Barbarian;
                case RacialSubsystemKind.HumanSpecialization:
                    return race == Race.Human;
                case RacialSubsystemKind.TieflingImplants:
                    return race == Race.Tiefling;
                case RacialSubsystemKind.ElfElementalContracts:
                    return race == Race.Elf;
                case RacialSubsystemKind.DwarfAncestry:
                    return race == Race.Dwarf;
                case RacialSubsystemKind.UndeadSkillTree:
                    return race == Race.Undead;
                case RacialSubsystemKind.BeastmanSoulBeast:
                    return race == Race.Beastman;
                case RacialSubsystemKind.DragonianSpells:
                    return race == Race.Dragonian;
                default:
                    return false;
            }
        }
    }
}
