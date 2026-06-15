using JRogue.World.Generation;

namespace JRogue.Racial
{
    public static class HumanPriestVowProgressService
    {
        public static int ResolveCurrentFloorIndex()
        {
            DungeonTimeService time = DungeonTimeService.Instance;
            if (time == null || !time.DungeonRunActive)
                return 0;

            return time.ResolveActiveFloorChainIndex();
        }

        public static int ResolveElapsedDayNightCycles()
        {
            DungeonTimeService time = DungeonTimeService.Instance;
            if (time == null || !time.DungeonRunActive)
                return 0;

            return time.ElapsedCycles;
        }
    }
}
