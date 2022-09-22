using Microsoft.Extensions.Logging;
using NodaTime;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
namespace HolidayService;

// get all holidays 2 weeks back and 4 weeks ahead
public sealed class Holidays
{
    private readonly Enrico Enrico;
    private readonly Dictionary<string, Dictionary<LocalDate, Holiday>> Dictionary = new();
    private static string MakeKey(string country, string region) => country + "-" + region;
    private readonly SemaphoreSlim Semaphore = new(1);

    public Holidays(ILogger<Holidays> logger, IClock clock)
    {
        Enrico = new Enrico(logger, clock);
    }

    public async Task LoadHolidays(string country, string region)
    {
        await Semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            string key = MakeKey(country, region);
            if (!Dictionary.ContainsKey(key))
            {
                JsonDocument json = await Enrico.GetHolidays(country, region).ConfigureAwait(false);
                Dictionary<LocalDate, Holiday> holidays = Holiday.GetHolidays(json);
                Dictionary.Add(key, holidays);
            }
        }
        finally
        {
            Semaphore.Release();
        }
    }

    public bool TryGetHoliday(string country, string region, LocalDate date, out Holiday holiday)
    {
        string key = MakeKey(country, region);

        if (Dictionary.TryGetValue(key, out Dictionary<LocalDate, Holiday>? holidays) && holidays.TryGetValue(date, out holiday))
            return true;

        holiday = Holiday.Undefined;
        return false;
    }
}
