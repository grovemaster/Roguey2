namespace JRogue.World.Generation
{
    public static class TownTimeLogic
    {
        public static bool IsPortalWindowDay(int calendarDayIndex) =>
            calendarDayIndex >= 1 && calendarDayIndex % 3 == 1;

        public static bool IsDungeonPortalOpen(int calendarDayIndex, TownTimePhase phase) =>
            phase == TownTimePhase.Morning && IsPortalWindowDay(calendarDayIndex);

        public static TownTimeAdvanceResult AdvancePhase(
            int calendarDayIndex,
            TownTimePhase currentPhase,
            out int newCalendarDayIndex,
            out TownTimePhase newPhase)
        {
            newCalendarDayIndex = calendarDayIndex;
            bool portalWindowClosed = IsDungeonPortalOpen(calendarDayIndex, currentPhase);

            switch (currentPhase)
            {
                case TownTimePhase.Morning:
                    newPhase = TownTimePhase.Day;
                    return new TownTimeAdvanceResult(
                        true,
                        false,
                        portalWindowClosed,
                        TownTimePhase.Morning,
                        newPhase);

                case TownTimePhase.Day:
                    newPhase = TownTimePhase.Night;
                    return new TownTimeAdvanceResult(
                        true,
                        false,
                        false,
                        TownTimePhase.Day,
                        newPhase);

                default:
                    newCalendarDayIndex = calendarDayIndex + 1;
                    newPhase = TownTimePhase.Morning;
                    return new TownTimeAdvanceResult(
                        true,
                        true,
                        false,
                        TownTimePhase.Night,
                        newPhase);
            }
        }

        public static string BuildPortalClosedMessage(int calendarDayIndex, TownTimePhase phase)
        {
            if (phase != TownTimePhase.Morning)
                return "The portal is dormant until morning.";

            if (!IsPortalWindowDay(calendarDayIndex))
            {
                return
                    $"The portal opens on every third dawn (days 1, 4, 7…). Today is day {calendarDayIndex}, {phase.ToString().ToLowerInvariant()}.";
            }

            return "The portal is closed.";
        }
    }
}
