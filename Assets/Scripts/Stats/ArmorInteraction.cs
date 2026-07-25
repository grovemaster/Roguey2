namespace JRogue.Stats
{
    /// <summary>
    /// How Armor Class applies to a hit. Orthogonal to <see cref="DamageType"/>.
    /// See Docs/Progression/Stat-Derivation-And-Combat-Scaling-Requirements.md §8.2.
    /// </summary>
    public enum ArmorInteraction : byte
    {
        /// <summary>Full AC mitigation (AC / k). Melee and physical projectiles.</summary>
        Full = 0,

        /// <summary>Reduced AC mitigation (AC / k_partial). Physical elemental hits (e.g. Fireball).</summary>
        Partial = 1,

        /// <summary>AC ignored. Status ticks, pure magic / psychic / force.</summary>
        None = 2
    }
}
