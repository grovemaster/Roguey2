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
}
