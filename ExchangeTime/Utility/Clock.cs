using NodaTime;
using System;

#nullable enable

namespace ExchangeTime.Utility
{
    internal static class Clock
    {
        internal static readonly DateTimeZone SystemTimeZone = DateTimeZoneProviders.Tzdb.GetSystemDefault();

        internal static Instant CurrentInstant
        {
            get
            {
                return SystemClock.Instance.GetCurrentInstant();
                /*
                var ldt = new LocalDateTime(2020, 3, 17, 14, 30, 0);
                var zone = SystemTimeZone;
                var zdt = zone.AtStrictly(ldt);
                return zdt.ToInstant();
                */
            }
        }

        internal static ZonedDateTime SystemTime => CurrentInstant.InZone(SystemTimeZone);

        internal static Instant Round(this Instant instant, long ticks) =>
            Instant.FromUnixTimeSeconds((instant.ToUnixTimeTicks() + ticks / 2) / ticks);
    }
}
