using System;
using System.Collections.Generic;

namespace JRogue.World.Generation
{
    [Serializable]
    public sealed class DungeonTimeRunState
    {
        public int ElapsedCycles;
        public int MaximumCycles = 7;
        public DungeonTimePhase CurrentPhase = DungeonTimePhase.Day;
        public int PhasePlayerTurnsElapsed;
        public string ActiveTimeFloorId;

        public readonly HashSet<string> AppliedAdditionalBudgetFloors = new HashSet<string>();

        public void ResetForNewRun(string firstFloorId, int baseDayNightCycles)
        {
            ElapsedCycles = 0;
            MaximumCycles = Math.Max(1, baseDayNightCycles);
            CurrentPhase = DungeonTimePhase.Day;
            PhasePlayerTurnsElapsed = 0;
            ActiveTimeFloorId = firstFloorId;
            AppliedAdditionalBudgetFloors.Clear();
        }
    }
}
