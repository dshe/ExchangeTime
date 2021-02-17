using Microsoft.Extensions.Logging.Abstractions;
using HolidayService;
using NodaTime;
using System;
using System.Text.Json;
using System.Threading.Tasks;

class Program
{
    static Holidays holidays = new(NullLogger<Holidays>.Instance, SystemClock.Instance);

    static async void Main(string[] args)
    {
        await holidays.LoadHolidays("usa", "ny");

        Holiday? holiday = holidays.TryGetHoliday("usa", "ny", new LocalDate(2019, 12, 25));
        if (holiday != null)
            Console.WriteLine(holiday.Name);

        await TestEnrico();

    }
    static async Task TestEnrico()
    {
        Enrico holidayService = new(NullLogger.Instance, SystemClock.Instance);

        ZonedDateTime now = SystemClock.Instance.GetCurrentInstant().InUtc();
        LocalDate from = now.Minus(Duration.FromDays(30)).Date;
        LocalDate to = now.Plus(Duration.FromDays(90)).Date;

        JsonDocument json = await holidayService.GetHolidays("usa", "ny");
    }
}
