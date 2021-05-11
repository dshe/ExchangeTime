using NodaTime;

namespace ExchangeTime.Utility
{
    public static class DateTimeExtenstions
    {
        private static bool IsIsrael(this string name) => (name == "Israel" || name == "isr");
        private static bool IsFriday(this LocalDateTime dt) => dt.DayOfWeek == IsoDayOfWeek.Friday;
        private static bool IsSaturday(this LocalDateTime dt) => dt.DayOfWeek == IsoDayOfWeek.Saturday;
        private static bool IsSunday(this LocalDateTime dt) => dt.DayOfWeek == IsoDayOfWeek.Sunday;

        public static int DayOfWeekend(this LocalDateTime dt, string country)
        {
            if (country.IsIsrael())
            {
                if (dt.IsFriday())
                    return 1;
                if (dt.IsSaturday())
                    return 2;
                return 0; // not a weekend
            }
            if (dt.IsSaturday())
                return 1;
            if (dt.IsSunday())
                return 2;
            return 0; // not a weekend
        }

        public static bool IsWeekend(this LocalDateTime dt, string country) =>
            dt.DayOfWeekend(country) != 0;
    }
}
