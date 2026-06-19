using JRogue.World.Generation;

namespace JRogue.World.Town
{
    public static class InnRestService
    {
        public static void SleepAtInn()
        {
            PartyTownRestService.RestoreFullParty();

            GameCalendarService calendar = GameCalendarService.Instance;
            if (calendar != null && calendar.IsEnabled)
                calendar.AdvanceDay(GameCalendarDayAdvanceSource.RestAtInn);
        }
    }
}
