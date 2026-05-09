namespace JRogue.Manager.Combat
{
    /// <summary>
    /// Party-level aggregate tension flag derived from LOS, remote sensing, and enemy pursuit AI.
    /// Inventory and other gameplay queries should treat this as the single authoritative source,
    /// not recomputed per-feature.
    /// </summary>
    public enum CombatTensionState
    {
        OutOfCombat,
        InCombat,
    }
}
