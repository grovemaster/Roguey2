namespace JRogue.World.Generation
{
    public static class GameCalendarLogic
    {
        public const int StartYear = 330;
        public const int DaysPerMonth = 30;
        public const int MonthsPerYear = 12;
        public const int DefaultPortalIntervalDays = 3;
        public const int DefaultPortalStartDay = 1;

        public static GameCalendarDate CreateNewRunStartDate() =>
            new GameCalendarDate(StartYear, 1, 1);

        public static int ToAbsoluteDayIndex(GameCalendarDate date) =>
            (date.Year - StartYear) * MonthsPerYear * DaysPerMonth
            + (date.Month - 1) * DaysPerMonth
            + (date.Day - 1);

        public static bool IsDungeonPortalDay(
            GameCalendarDate date,
            int portalIntervalDays,
            int portalStartDay = DefaultPortalStartDay)
        {
            if (portalIntervalDays < 1 || portalStartDay < 1)
                return false;

            int absoluteDayIndex = ToAbsoluteDayIndex(date);
            return (absoluteDayIndex - (portalStartDay - 1)) % portalIntervalDays == 0;
        }

        public static GameCalendarDate AdvanceOneDay(GameCalendarDate date)
        {
            int day = date.Day + 1;
            int month = date.Month;
            int year = date.Year;

            if (day > DaysPerMonth)
            {
                day = 1;
                month++;
            }

            if (month > MonthsPerYear)
            {
                month = 1;
                year++;
            }

            return new GameCalendarDate(year, month, day);
        }

        public static string FormatDisplayDate(GameCalendarDate date) =>
            $"Year {date.Year} · Month {date.Month} · Day {date.Day}";

        public static string BuildPortalClosedMessage(
            GameCalendarDate date,
            int portalIntervalDays,
            int portalStartDay = DefaultPortalStartDay)
        {
            if (IsDungeonPortalDay(date, portalIntervalDays, portalStartDay))
                return "The portal is closed.";

            return
                $"The portal opens every {portalIntervalDays} days (starting day {portalStartDay}). " +
                $"Today is {FormatDisplayDate(date)}.";
        }
    }
}
