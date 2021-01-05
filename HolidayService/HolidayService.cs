using NodaTime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
/*
need 2 weeks back and 4 weeks ahead
*/

namespace HolidayService
{
    public class Holidays
    {
        private const int MaxAgeDays = 10, LeadDays = 90, LagDays= 30;
        private readonly IClock Clock;
        private readonly Enrico Enrico;
        private readonly Dictionary<string, Dictionary<LocalDate, Holiday>> Dictionary = new Dictionary<string, Dictionary<LocalDate, Holiday>>();
        private string MakeKey(string country, string region) => country + "-" + region;
        private readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1);

        public Holidays(IClock clock)
        {
            Clock = clock;
            Enrico = new Enrico(clock, MaxAgeDays);
        }

        public async Task LoadHolidays(string country, string region)
        {
            var key = MakeKey(country, region);

            await Semaphore.WaitAsync().ConfigureAwait(false);

            if (!Dictionary.TryGetValue(key, out var holidays))
            {
                var json = await GetHolidays(country, region).ConfigureAwait(false);
                var root = JsonDocument.Parse(json).RootElement;
                if (root.ValueKind != JsonValueKind.Array)
                {
                    if (root.ValueKind != JsonValueKind.Object && root.TryGetProperty("error", out var val))
                        throw new InvalidDataException(val.GetString());
                    throw new InvalidDataException($"Unknown error parsing: {root}.");
                }
                holidays = root.EnumerateArray().Select(j => new Holiday(j)).ToDictionary(h => h.Date, h => h);
                Dictionary.Add(key, holidays);
            }

            Semaphore.Release();
        }

        private async Task<string> GetHolidays(string country, string region)
        {
            var now = Clock.GetCurrentInstant().InUtc();
            var from = now.Minus(Duration.FromDays(LagDays)) .Date;
            var to   = now.Plus (Duration.FromDays(LeadDays)).Date;
            return await Enrico.GetHolidays(country, region, from, to);
        }

        public Holiday? TryGetHoliday(string country, string region, LocalDate date)
        {
            var key = MakeKey(country, region);

            if (!Dictionary.TryGetValue(key, out var holidays))
                return null;

            //return holidays.GetValueOrDefault(date, null);
            return holidays.GetValueOrDefault(date);
        }

    }
}
