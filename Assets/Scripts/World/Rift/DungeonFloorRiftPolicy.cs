using System;
using JRogue.World.Altar;
using UnityEngine;

namespace JRogue.World.Rift
{
    /// <summary>Per-host-floor rift portal policy (Docs/World/Rift-Requirements.md §3.2).</summary>
    [Serializable]
    public sealed class DungeonFloorRiftPolicy
    {
        public RiftDefinition[] rifts = Array.Empty<RiftDefinition>();
        [Tooltip("Northern Dark pedestal offering altar (replaces bump flavor).")]
        public AltarDefinition riftPedestalAltar;
        [Min(1)] public int maxRiftPortalsPerRun = 1;
        [Min(0)] public int minDungeonRunsBetweenPortals = 3;
        [Min(1)] public int minDungeonDayToOpenPortal = 2;
        [Min(0)] public int minDungeonRunsBeforeWandering = 5;
        [Min(1)] public int riftPortalOpenTurns = 30;
        [Min(0)] public int wanderingRespawnDelayTurns = 20;

        public bool HasRifts => rifts != null && rifts.Length > 0;

        public RiftDefinition PickRandomRift(System.Random rng)
        {
            if (!HasRifts)
                return null;
            if (rifts.Length == 1)
                return rifts[0];
            int index = rng.Next(0, rifts.Length);
            return rifts[index];
        }
    }
}
