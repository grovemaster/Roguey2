using System;

namespace JRogue.Stats.Racial
{
    /// <summary>Non-physical racial traits (folk baseline, blessings, quests). Distinct from <see cref="BodyCapabilityFlags"/>.</summary>
    [Flags]
    public enum RacialTraitFlags : uint
    {
        None = 0,
        WarriorWillpower = 1 << 0,
    }
}
