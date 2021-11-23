using NodaTime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace HolidayService;

public class Holiday
{
    public LocalDate Date { get; }
    public string Name { get; }
    private Holiday(LocalDate date, string name) => (Date, Name) = (date, name);

    private static Holiday Create(JsonElement json)
    {
        if (!json.TryGetProperty("observedOn", out JsonElement d))
            d = json.GetProperty("date");

        LocalDate date = new(
            d.GetProperty("year").GetInt32(),
            d.GetProperty("month").GetInt32(),
            d.GetProperty("day").GetInt32());

        // get English name of holiday
        string name = json.GetProperty("name")
            .EnumerateArray()
            .Where(x => x.GetProperty("lang").GetString() == "en")
            .Single()
            .GetProperty("text")
            .GetString() ?? throw new InvalidDataException("Missing property: 'text'");

        return new Holiday(date, name);
    }

    public static Dictionary<LocalDate, Holiday> GetHolidays(JsonDocument json)
    {
        ArgumentNullException.ThrowIfNull(json);

        return json.RootElement
            .EnumerateArray()
            .Select(j => Create(j))
            .GroupBy(h => h.Date) // in case there is more than one holiday on a particular date
            .Select(g => g.First())
            .ToDictionary(h => h.Date, h => h);
    }
}
