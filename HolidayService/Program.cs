using Microsoft.Extensions.Logging;
using NodaTime;
using System;
using System.Threading.Tasks;

namespace HolidayService
{
    class Program
    {
        static readonly IClock Clock = SystemClock.Instance;

        static async Task Main()
        {
            Holidays holidays = new(Clock);

            await holidays.LoadHolidays("usa", "ny");

            Holiday? holiday = holidays.TryGetHoliday("usa", "ny", new LocalDate(2019, 12, 25));
            if (holiday != null)
                Console.WriteLine(holiday.Name);

            await TestEnrico();
        }

        static async Task TestEnrico()
        {
            //logger.Log(LogLevel.Information, "rrr");

            Enrico holidayService = new(SystemClock.Instance, 1);

            ZonedDateTime now = Clock.GetCurrentInstant().InUtc();
            LocalDate from = now.Minus(Duration.FromDays(30)).Date;
            LocalDate to = now.Plus(Duration.FromDays(90)).Date;

            string str = await holidayService.GetHolidays("usa", "ny", from, to);

            //ILogger logger = new LoggerFactory().CreateLogger<Program>();
            //logger.LogInformation("Example log message");
        }

    }
}
