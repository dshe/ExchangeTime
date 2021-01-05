using NodaTime;
using System;

namespace ExchangeTime
{
    public static class DateTimeExtenstions
    {
        public static bool IsFriday(this LocalDateTime dt) =>
            dt.DayOfWeek == IsoDayOfWeek.Friday;

        public static bool IsSaturday(this LocalDateTime dt) =>
            dt.DayOfWeek == IsoDayOfWeek.Saturday;

        public static bool IsSunday(this LocalDateTime dt) =>
            dt.DayOfWeek == IsoDayOfWeek.Sunday;

        public static bool IsIsrael(this string name) => (name == "Israel" || name == "isr");

        public static bool IsWeekend(this LocalDateTime dt, string country) =>
            country.IsIsrael() ? (dt.IsFriday() || dt.IsSaturday()) : (dt.IsSaturday() || dt.IsSunday());

        public static int? DayOfWeekend(this LocalDateTime dt, string country)
        {
            if (country.IsIsrael())
            {
                if (dt.IsFriday())
                    return 1;
                if (dt.IsSaturday())
                    return 2;
            }
            else
            {
                if (dt.IsSaturday())
                    return 1;
                if (dt.IsSunday())
                    return 2;
            }
            return null;
        }

    }
}
