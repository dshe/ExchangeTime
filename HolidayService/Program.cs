/*
using HolidayService;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using System;
using System.Text.Json;
using System.Threading.Tasks;
namespace main;

public static class Program
{
    static readonly Holidays holidays = new(NullLogger<Holidays>.Instance, SystemClock.Instance);

    static async Task Main()
    {
        //await holidays.LoadHolidays("usa", "ny");
        await holidays.LoadHolidays("isl", "");

        //if (holidays.TryGetHoliday("usa", "ny", new LocalDate(2021, 12, 25), out Holiday holiday))
        if (holidays.TryGetHoliday("isl", "", new LocalDate(2021, 12, 25), out Holiday holiday))
            Console.WriteLine(holiday.Name);

        await TestEnrico().ConfigureAwait(false);
    }

    static async Task TestEnrico()
    {
        Enrico holidayService = new(NullLogger.Instance, SystemClock.Instance);

        ZonedDateTime now = SystemClock.Instance.GetCurrentInstant().InUtc();
        LocalDate from = now.Minus(Duration.FromDays(30)).Date;
        LocalDate to = now.Plus(Duration.FromDays(90)).Date;

        JsonDocument json = await holidayService.GetHolidays("usa", "ny").ConfigureAwait(false);
    }

}
*/
