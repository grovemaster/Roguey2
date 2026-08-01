namespace JRogue.Stats.Racial
{
    /// <summary>
    /// Whether choices within a race-specific subsystem can be changed after they are made.
    /// </summary>
    public enum RacialCommitmentPolicy : byte
    {
        /// <summary>No racial progression subsystem, or policy not applicable.</summary>
        NotApplicable = 0,

        /// <summary>Choices are permanent (e.g. Barbarian Spirit Imprint branches).</summary>
        Permanent = 1,

        /// <summary>Choices can be respec'd per design rules (e.g. Tiefling implants).</summary>
        RespecAllowed = 2
    }

    /// <summary>
    /// Which layer a modifier belongs to for refresh ordering and debugging.
    /// <see cref="Stat.GetValue"/> still sums all layers additively (Phase 0).
    /// </summary>
    public enum ModifierSourceLayer : byte
    {
        Base = 0,
        RacialLoadout = 10,
        RacialProgression = 20,
        PermanentConsumable = 25,
        Equipment = 30,
        Essence = 40,
        Temporary = 50
    }

    /// <summary>
    /// Phase 0 stacking and evaluation contracts. See Docs/RacialSystem/Phase0-Glossary-And-Data-Contracts.md.
    /// </summary>
    public static class RacialStackingContract
    {
        public const int CurrentIdentitySnapshotVersion = 1;

        public static readonly ModifierSourceLayer[] ModifierEvaluationOrder =
        {
            ModifierSourceLayer.Base,
            ModifierSourceLayer.RacialLoadout,
            ModifierSourceLayer.RacialProgression,
            ModifierSourceLayer.PermanentConsumable,
            ModifierSourceLayer.Equipment,
            ModifierSourceLayer.Essence,
            ModifierSourceLayer.Temporary
        };

        public const string CrossSourceStackingRule =
            "Distinct modifier sources sum in Stat.GetValue(); racial does not special-case duplicate items.";
    }
}
