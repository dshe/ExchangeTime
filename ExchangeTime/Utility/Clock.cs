using NodaTime;


namespace ExchangeTime
{
    public class Clock : IClock
    {
        public static IDateTimeZoneProvider DateTimeZoneProvider => DateTimeZoneProviders.Tzdb;
        public static DateTimeZone SystemTimeZone => DateTimeZoneProvider.GetZoneOrNull("America/New_York");
        //DateTimeZoneProvider.GetSystemDefault();

        public ZonedDateTime GetSystemZonedDateTime() =>
            GetCurrentInstant().InZone(SystemTimeZone);

        private static Instant Round(Instant instant, long ticks) =>
            Instant.FromUnixTimeSeconds((instant.ToUnixTimeTicks() + ticks / 2) / ticks);

        public Instant GetCurrentInstant()
        {
            var instant = SystemClock.Instance.GetCurrentInstant();
            return Round(instant, NodaConstants.TicksPerSecond);
            /*
            var ldt = new LocalDateTime(2020, 3, 17, 14, 30, 0);
            var zone = SystemTimeZone;
            var zdt = zone.AtStrictly(ldt);
            return zdt.ToInstant();
            */
        }
    }
}
