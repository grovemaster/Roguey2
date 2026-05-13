using System;
using JRogue.Stats;

namespace JRogue.Stats.Racial
{
    /// <summary>
    /// Serializable identity slice for save games and networking. Does not include Spirit Imprint node ids
    /// (those live in a subsystem-specific blob in a later phase).
    /// </summary>
    [Serializable]
    public struct RacialIdentitySnapshot
    {
        /// <summary>Schema version for migration.</summary>
        public byte snapshotVersion;

        public Race race;
        public HumanClass humanClass;
        public RacialSubsystemKind subsystemKind;
        public BodyCapabilityFlags bodyCapabilities;

        public static RacialIdentitySnapshot CreateDefaultHuman()
        {
            return new RacialIdentitySnapshot
            {
                snapshotVersion = 1,
                race = Race.Human,
                humanClass = HumanClass.None,
                subsystemKind = RacialSubsystemKind.None,
                bodyCapabilities = BodyCapabilityFlags.None
            };
        }
    }
}
