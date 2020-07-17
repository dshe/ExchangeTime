using NodaTime;
using NodaTime.Text;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

/*
Enrico Service 2.0: http://kayaposoft.com/enrico/json/
(alternative: Calendarific: 1000 API Requests/Month)
*/

namespace HolidayService
{
    internal class Enrico
    {
        private const string FolderName = "Holidays";
        private readonly IClock Clock;
        private LocalDate Today => Clock.GetCurrentInstant().InUtc().Date;
        private readonly LocalDatePattern DatePattern = LocalDatePattern.CreateWithInvariantCulture("dd-MM-yyyy");
        private readonly HttpClient HttpClient = new HttpClient() { Timeout = new TimeSpan(0, 1, 10) };
        private readonly int MaxAgeDays;
        public Enrico(IClock clock, int maxAgeDays)
        {
            Clock = clock;
            ServicePointManager.UseNagleAlgorithm = false;
            ServicePointManager.Expect100Continue = false;
            //ServicePointManager.DefaultConnectionLimit = int.MaxValue;
            if (!Directory.Exists(FolderName))
                Directory.CreateDirectory(FolderName);
            MaxAgeDays = maxAgeDays;
        }

        internal async Task<string> GetHolidays(string country, string region, LocalDate fromDate, LocalDate toDate)
        {
            var fileName = MakeFileName(country, region);
            if (File.Exists(fileName))
            {
                var age = Today - LocalDate.FromDateTime(File.GetLastWriteTime(fileName));
                if (age.Days < MaxAgeDays)
                    return File.ReadAllText(fileName);
            }

            var str = await Download(country, region, fromDate, toDate).ConfigureAwait(false);

            var doc = JsonDocument.Parse(str).RootElement;
            if (doc.ValueKind == JsonValueKind.Array)
            {
                File.WriteAllText(fileName, str);
                return str;
            }
            // check for error: { "error":"Country 'usax' is not supported"}
            if (doc.ValueKind == JsonValueKind.Object && doc.TryGetProperty("error", out var val))
            {
                //Logger.LogCritical($"{val.GetString()}. Could not Parse: {str}.");
                throw new InvalidDataException(val.GetString());
            }

            //Logger.LogCritical($"Unknown error. Could not parse: {str}.");
            throw new InvalidDataException($"Unknown error parsing: {str}.");
        }

        private string MakeFileName(string country, string region)
        {
            var location = country;
            if (!string.IsNullOrWhiteSpace(region))
                location = $"{country}-{region}";
            return $"{FolderName}/holidays-{location}.json";
        }

        private async Task<string> Download(string country, string region, LocalDate fromDate, LocalDate toDate)
        {
            var url = $"https://kayaposoft.com/enrico/json/v2.0" +
                $"?action=getHolidaysForDateRange" +
                $"&fromDate={DatePattern.Format(fromDate)}" +
                $"&toDate={DatePattern.Format(toDate)}" +
                $"&country={country}" +
                $"&region={region}" +
                $"&holidayType=public_holiday";

            Debug.WriteLine($"Downloading: {url}.");

            return await HttpClient.GetStringAsync(url).ConfigureAwait(false);
        }
    }
}
