using System;

namespace JRogue.Stats.Racial
{
    /// <summary>
    /// Mutable anatomy / capability tags used for equipment legality and overrides (essences, curses, artifacts).
    /// Complements static rules from the folk loadout. Evaluated together when resolving <c>CanEquip</c>.
    /// </summary>
    [Flags]
    public enum BodyCapabilityFlags : uint
    {
        None = 0,

        /// <summary>Example: stature small enough to qualify for certain human gear tags.</summary>
        ReducedStature = 1 << 0,

        /// <summary>Example: horns absent or retracted; may allow helmets.</summary>
        NoHorns = 1 << 1
    }
}
