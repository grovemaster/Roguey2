using System;
using UnityEngine;

namespace JRogue.World.Lighting
{
    [Serializable]
    public struct AmbientPhaseScheduleEntry
    {
        [Range(LightLevel.Min, LightLevel.Max)]
        public int ambientLight;

        [Min(1)]
        public int durationTurns;
    }

    /// <summary>
    /// Runtime ambient region (overhead / day–night). Phase E wires turn ticks to <see cref="Phases"/>.
    /// </summary>
    [Serializable]
    public sealed class AmbientRegion
    {
        public int Id;
        public int CurrentAmbientLight;

        [Tooltip("Sum of phase durations; 0 when no cycle is configured.")]
        public int CycleLengthTurns;

        public AmbientPhaseScheduleEntry[] Phases = Array.Empty<AmbientPhaseScheduleEntry>();

        public int PhaseIndex;
        public int TurnsUntilNextPhase;

        public bool HasCycle => Phases != null && Phases.Length > 0 && CycleLengthTurns > 0;
    }
}
