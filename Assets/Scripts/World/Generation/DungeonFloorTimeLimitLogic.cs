namespace JRogue.World.Generation
{
    /// <summary>
    /// Per-floor day–night limits on the shared global <see cref="DungeonTimeRunState.ElapsedCycles"/> counter.
    /// See Docs/World/Dungeon-Floor-Time-Limits-Requirements.md.
    /// </summary>
    public static class DungeonFloorTimeLimitLogic
    {
        /// <summary>
        /// When &gt; 0, this floor uses an explicit per-floor limit; 0 means legacy run-wide <c>maximumCycles</c> only.
        /// </summary>
        public static int GetAuthoringLimit(DungeonFloorDefinition floor) =>
            floor != null ? floor.FloorDayNightCycleLimit : 0;

        public static bool UsesPerFloorLimit(DungeonFloorDefinition floor) =>
            GetAuthoringLimit(floor) > 0;

        /// <summary>True when portals to this floor must not transition.</summary>
        public static bool IsFloorExpiredForPortal(DungeonFloorDefinition floor, int elapsedCycles)
        {
            if (floor == null || !floor.ParticipatesInDungeonTime)
                return false;

            int limit = GetAuthoringLimit(floor);
            if (limit <= 0)
                return false;

            return elapsedCycles >= limit;
        }

        /// <summary>True when the party on this floor should be forced to town after a completed cycle.</summary>
        public static bool IsActiveFloorTimeExpired(DungeonFloorDefinition activeFloor, int elapsedCycles)
        {
            if (activeFloor == null || !activeFloor.ParticipatesInDungeonTime)
                return false;

            if (UsesPerFloorLimit(activeFloor))
                return elapsedCycles >= activeFloor.FloorDayNightCycleLimit;

            return false;
        }

        public static int ResolveDisplayCycleLimit(DungeonFloorDefinition activeFloor, int legacyMaximumCycles)
        {
            if (activeFloor != null && UsesPerFloorLimit(activeFloor))
                return activeFloor.FloorDayNightCycleLimit;

            return legacyMaximumCycles < 1 ? 1 : legacyMaximumCycles;
        }
    }
}
