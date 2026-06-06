namespace JRogue.World.Generation
{
    public readonly struct TownTimeAdvanceResult
    {
        public bool Advanced { get; }
        public bool CalendarDayChanged { get; }
        public bool PortalWindowClosed { get; }
        public TownTimePhase PreviousPhase { get; }
        public TownTimePhase NewPhase { get; }

        public TownTimeAdvanceResult(
            bool advanced,
            bool calendarDayChanged,
            bool portalWindowClosed,
            TownTimePhase previousPhase,
            TownTimePhase newPhase)
        {
            Advanced = advanced;
            CalendarDayChanged = calendarDayChanged;
            PortalWindowClosed = portalWindowClosed;
            PreviousPhase = previousPhase;
            NewPhase = newPhase;
        }
    }
}
