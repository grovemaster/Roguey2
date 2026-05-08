using System;

namespace JRogue.Core.Actor
{
    /// <summary>
    /// Categorical "what kind of being is this?" classification used for
    /// detection filters (radar, smite-targeting, banishment, etc.).
    /// Modeled on DCSS's <c>mon_holy_type</c> bitfield: each actor typically
    /// occupies a single bit, while filters may combine multiple bits.
    /// </summary>
    [Flags]
    public enum EssenceType
    {
        None = 0,
        Life = 1 << 0,
        Undead = 1 << 1,
        Demonic = 1 << 2,
        Holy = 1 << 3,
        Mechanical = 1 << 4,
        Plant = 1 << 5,
        Elemental = 1 << 6,

        Any = Life | Undead | Demonic | Holy | Mechanical | Plant | Elemental
    }
}
