namespace JRogue.Stats.Racial
{
    /// <summary>
    /// Optional one-way specialization for <see cref="Race.Human"/> only. See Human-Class-Powers-Requirements.md.
    /// </summary>
    public enum HumanClass : byte
    {
        /// <summary>Civilian / no commitment — essences and Soul Power as default Human.</summary>
        None = 0,

        Knight = 1,
        Mage = 2,
        Priest = 3
    }
}
