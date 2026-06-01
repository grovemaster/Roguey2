namespace JRogue.World.Generation
{
    public readonly struct DungeonTimeTickResult
    {
        public bool PhaseAdvanced { get; }
        public bool CycleCompleted { get; }
        public bool TimeExpired { get; }
        public DungeonTimePhase NewPhase { get; }

        public DungeonTimeTickResult(
            bool phaseAdvanced,
            bool cycleCompleted,
            bool timeExpired,
            DungeonTimePhase newPhase)
        {
            PhaseAdvanced = phaseAdvanced;
            CycleCompleted = cycleCompleted;
            TimeExpired = timeExpired;
            NewPhase = newPhase;
        }
    }

    public static class DungeonTimeLogic
    {
        public static int GetPhaseTurnLimit(DungeonFloorDefinition floor, DungeonTimePhase phase)
        {
            if (floor == null)
                return 1;

            int limit = phase == DungeonTimePhase.Day
                ? floor.PlayerTurnsPerDay
                : floor.PlayerTurnsPerNight;

            return limit < 1 ? 1 : limit;
        }

        public static DungeonTimeTickResult AdvancePlayerTurn(
            DungeonTimeRunState state,
            DungeonFloorDefinition activeFloor)
        {
            if (state == null)
                return new DungeonTimeTickResult(false, false, false, DungeonTimePhase.Day);

            state.PhasePlayerTurnsElapsed++;
            int limit = GetPhaseTurnLimit(activeFloor, state.CurrentPhase);

            if (state.PhasePlayerTurnsElapsed < limit)
                return new DungeonTimeTickResult(false, false, false, state.CurrentPhase);

            state.PhasePlayerTurnsElapsed = 0;
            bool cycleCompleted;
            DungeonTimePhase newPhase;

            if (state.CurrentPhase == DungeonTimePhase.Day)
            {
                state.CurrentPhase = DungeonTimePhase.Night;
                newPhase = DungeonTimePhase.Night;
                cycleCompleted = false;
            }
            else
            {
                state.CurrentPhase = DungeonTimePhase.Day;
                newPhase = DungeonTimePhase.Day;
                state.ElapsedCycles++;
                cycleCompleted = true;
            }

            bool expired = cycleCompleted && state.ElapsedCycles >= state.MaximumCycles;
            return new DungeonTimeTickResult(true, cycleCompleted, expired, newPhase);
        }

        public static void ApplyFirstVisitBudget(
            DungeonTimeRunState state,
            DungeonFloorDefinition floor,
            bool isFirstFloorInChain,
            bool isFirstVisit)
        {
            if (state == null || floor == null || !isFirstVisit)
                return;

            if (isFirstFloorInChain)
            {
                state.MaximumCycles = floor.BaseDayNightCycles < 1 ? 1 : floor.BaseDayNightCycles;
                return;
            }

            if (!floor.ParticipatesInDungeonTime)
                return;

            if (state.AppliedAdditionalBudgetFloors.Contains(floor.FloorId))
                return;

            state.MaximumCycles += floor.AdditionalDayNightCycles;
            state.AppliedAdditionalBudgetFloors.Add(floor.FloorId);
        }
    }
}
