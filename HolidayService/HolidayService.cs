using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace HolidayService;

// get all holidays 2 weeks back and 4 weeks ahead
public sealed class Holidays : IDisposable
{
    private readonly Enrico Enrico;
    private readonly Dictionary<string, Dictionary<LocalDate, Holiday>> Dictionary = new();
    private static string MakeKey(string country, string region) => country + "-" + region;

    public Holidays(ILogger<Holidays> logger, IClock clock)
    {
        Enrico = new Enrico(logger, clock);
    }

    // not thread safe
    public async Task LoadAllHolidays(IEnumerable<(string country, string region)> locations, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(locations);
        foreach (var (country, region) in locations)
            await LoadHolidays(country, region, ct).ConfigureAwait(false);
    }

    private async Task LoadHolidays(string country, string region, CancellationToken ct)
    {
        string key = MakeKey(country, region);
        if (!Dictionary.ContainsKey(key))
        {
            JsonDocument json = await Enrico.GetHolidays(country, region, ct).ConfigureAwait(false);
            Dictionary<LocalDate, Holiday> holidays = Holiday.GetHolidays(json);
            Dictionary.Add(key, holidays);
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

    public void Dispose() => Enrico.Dispose();
}
