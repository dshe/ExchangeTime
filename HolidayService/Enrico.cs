using Microsoft.Extensions.Logging;
using NodaTime.Text;
using System.IO;
using System.Net.Http;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;
namespace HolidayService;
/*
Enrico Service 2.0: http://kayaposoft.com/enrico/json/
(alternative: Calendarific: 1000 API Requests/Month)
*/

internal sealed class Enrico : IDisposable
{
    private const string FolderName = "Holidays";
    private readonly IClock Clock;
    private readonly ILogger Logger;
    private readonly LocalDatePattern DatePattern = LocalDatePattern.CreateWithInvariantCulture("dd-MM-yyyy");
    private readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(30) };
    private readonly JsonSerializerOptions JsonSerializerOptions;
    public Enrico(ILogger logger, IClock clock)
    {
        Logger = logger;
        Clock = clock;
        if (!Directory.Exists(FolderName))
            Directory.CreateDirectory(FolderName);

        JavaScriptEncoder customEncoder = JavaScriptEncoder.Create(new TextEncoderSettings(UnicodeRanges.All));
        JsonSerializerOptions = new() { WriteIndented = true, Encoder = customEncoder };
    }

    public async Task<JsonDocument> GetHolidays(string country, string region, CancellationToken ct)
    {
        const int MaxAgeDays = 10;
        string fileName = MakeFileName(country, region);

        if (File.Exists(fileName))
        {
            LocalDate today = Clock.GetCurrentInstant().InUtc().Date;
            Period age = today - LocalDate.FromDateTime(File.GetLastWriteTimeUtc(fileName));
            if (age.Days < MaxAgeDays)
                return ReadJson();
        }

        try
        {
            JsonDocument json = await Download(country, region, ct).ConfigureAwait(false);
            SaveJson(json);
            return json;
        }
        catch (HttpRequestException)
        {
            if (File.Exists(fileName))
                return ReadJson();
            throw;
        }

        void SaveJson(JsonDocument json)
        {
            string str = JsonSerializer.Serialize(json, JsonSerializerOptions);
            File.WriteAllText(fileName, str);
        }

        JsonDocument ReadJson()
        {
            string txt = File.ReadAllText(fileName);
            return JsonDocument.Parse(txt);
        }
    }

    private static string MakeFileName(string country, string region)
    {
        if (string.IsNullOrWhiteSpace(country))
            throw new InvalidOperationException("Invalid country.");
        string location = country;
        if (!string.IsNullOrWhiteSpace(region))
            location = $"{country}-{region}";
        return $"{FolderName}/holidays-{location}.json";
    }

    private async Task<JsonDocument> Download(string country, string region, CancellationToken ct)
    {
        const int LeadDays = 90, LagDays = 30;
        Instant instant = Clock.GetCurrentInstant();
        LocalDate from = instant.Minus(Duration.FromDays(LagDays)).InUtc().Date;
        LocalDate to = instant.Plus(Duration.FromDays(LeadDays)).InUtc().Date;
        // use UTC rather than actual zones, for simplicicity

        string url = $"https://kayaposoft.com/enrico/json/v2.0" +
            $"?action=getHolidaysForDateRange" +
            $"&fromDate={DatePattern.Format(from)}" +
            $"&toDate={DatePattern.Format(to)}" +
            $"&country={country}" +
            $"&region={region}" +
            $"&holidayType=public_holiday";

        Logger.LogInformation("Downloading: {Url}.", url);

        Uri uri = new(url);
        string str = await HttpClient.GetStringAsync(uri, ct).ConfigureAwait(false);

        JsonDocument json = JsonDocument.Parse(str);
        JsonElement root = json.RootElement;

        if (root.ValueKind == JsonValueKind.Array) // format is correct
            return json;

        Logger.LogCritical("Unknown error. Could not parse: {Str}.", str);

        // check for error: { "error":"Country 'usax' is not supported"}
        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("error", out JsonElement val))
        {
            Logger.LogCritical("{Value}.", val.GetString());
            throw new InvalidDataException(val.GetString());
        }

        throw new InvalidDataException($"Unknown error parsing: {str}.");
    }

    public void Dispose() => HttpClient.Dispose();
}
