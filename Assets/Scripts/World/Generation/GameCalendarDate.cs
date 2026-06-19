using System;

namespace JRogue.World.Generation
{
    [Serializable]
    public struct GameCalendarDate : IEquatable<GameCalendarDate>
    {
        public int Year;
        public int Month;
        public int Day;

        public GameCalendarDate(int year, int month, int day)
        {
            Year = year;
            Month = month;
            Day = day;
        }

        public bool Equals(GameCalendarDate other) =>
            Year == other.Year && Month == other.Month && Day == other.Day;

        public override bool Equals(object obj) => obj is GameCalendarDate other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Year, Month, Day);
    }
}
