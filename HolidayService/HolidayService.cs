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
get all holidays 2 weeks back and 4 weeks ahead
*/

namespace HolidayService
{
    public class Holidays
    {
        private const int MaxAgeDays = 10, LeadDays = 90, LagDays= 30;
        private readonly IClock Clock;
        private readonly Enrico Enrico;
        private readonly Dictionary<string, Dictionary<LocalDate, Holiday>> Dictionary = new();
        private static string MakeKey(string country, string region) => country + "-" + region;
        private readonly SemaphoreSlim Semaphore = new(1);

        public Holidays(IClock clock)
        {
            Clock = clock;
            Enrico = new Enrico(clock, MaxAgeDays);
        }

        public async Task LoadHolidays(string country, string region)
        {
            string key = MakeKey(country, region);

            await Semaphore.WaitAsync().ConfigureAwait(false);

            if (!Dictionary.ContainsKey(key))
            {
                string json = await GetHolidays(country, region).ConfigureAwait(false);
                JsonElement root = JsonDocument.Parse(json).RootElement;
                if (root.ValueKind != JsonValueKind.Array)
                {
                    if (root.ValueKind != JsonValueKind.Object && root.TryGetProperty("error", out JsonElement val))
                        throw new InvalidDataException(val.GetString());
                    throw new InvalidDataException($"Unknown error parsing: {root}.");
                }

                // this can be done with linq extension
                Dictionary<LocalDate, Holiday> holidays = new();
                foreach (JsonElement j in root.EnumerateArray())
                {
                    Holiday holiday = new(j);
                    // there may be more than one holiday on a particular date
                    if (!holidays.ContainsKey(holiday.Date)) 
                        holidays.Add(holiday.Date, holiday);
                }

                Dictionary.Add(key, holidays);
            }

            Semaphore.Release();
        }

        private async Task<string> GetHolidays(string country, string region)
        {
            ZonedDateTime now = Clock.GetCurrentInstant().InUtc();
            LocalDate from = now.Minus(Duration.FromDays(LagDays)) .Date;
            LocalDate to   = now.Plus (Duration.FromDays(LeadDays)).Date;
            return await Enrico.GetHolidays(country, region, from, to);
        }

        public Holiday? TryGetHoliday(string country, string region, LocalDate date)
        {
            string key = MakeKey(country, region);

            if (!Dictionary.TryGetValue(key, out Dictionary<LocalDate, Holiday>? holidays))
                return null;

            return holidays?.GetValueOrDefault(date);
        }
    }
}
