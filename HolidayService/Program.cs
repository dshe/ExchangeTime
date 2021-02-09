using Microsoft.Extensions.Logging.Abstractions;
using HolidayService;
using NodaTime;
using System;
using System.Text.Json;
using System.Threading.Tasks;

Holidays holidays = new(SystemClock.Instance, NullLoggerFactory.Instance);

await holidays.LoadHolidays("usa", "ny");

Holiday? holiday = holidays.TryGetHoliday("usa", "ny", new LocalDate(2019, 12, 25));
if (holiday != null)
    Console.WriteLine(holiday.Name);

await TestEnrico();

static async Task TestEnrico()
{
    //logger.Log(LogLevel.Information, "rrr");

    Enrico holidayService = new(SystemClock.Instance, NullLogger.Instance);

    ZonedDateTime now = SystemClock.Instance.GetCurrentInstant().InUtc();
    LocalDate from = now.Minus(Duration.FromDays(30)).Date;
    LocalDate to = now.Plus(Duration.FromDays(90)).Date;

    JsonDocument json = await holidayService.GetHolidays("usa", "ny");

    //ILogger logger = new LoggerFactory().CreateLogger<Program>();
    //logger.LogInformation("Example log message");
}
