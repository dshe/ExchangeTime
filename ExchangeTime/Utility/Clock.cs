using NodaTime;
using System;

namespace ExchangeTime.Utility
{
    public static class Clock
    {
        public static readonly DateTimeZone SystemTimeZone = DateTimeZoneProviders.Tzdb.GetSystemDefault();

        public static Instant CurrentInstant
        {
            get
            {
                return SystemClock.Instance.GetCurrentInstant();
                /*
                var ldt = new LocalDateTime(2020, 6, 10, 14, 0, 0);
                var zone = SystemTimeZone;
                var zdt = zone.AtStrictly(ldt);
                return zdt.ToInstant();
                */
            }
        }

        public static ZonedDateTime SystemTime => CurrentInstant.InZone(SystemTimeZone);

        public static Instant Round(this Instant instant, long ticks) =>
            Instant.FromUnixTimeSeconds((instant.ToUnixTimeTicks() + ticks / 2) / ticks);
    }
}
