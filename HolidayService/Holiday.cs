using NodaTime;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace HolidayService
{
    public class Holiday
    {
        public LocalDate Date { get; }
        public string Name { get; }

        internal Holiday(JsonElement json)
        {
            if (!json.TryGetProperty("observedOn", out JsonElement d))
                d = json.GetProperty("date");

            Date = new LocalDate(
                d.GetProperty("year").GetInt32(),
                d.GetProperty("month").GetInt32(),
                d.GetProperty("day").GetInt32());

            // get English name of holiday
            Name = json.GetProperty("name")
                .EnumerateArray()
                .Where(x => x.GetProperty("lang").GetString() == "en")
                .Single()
                .GetProperty("text")
                .GetString() ?? throw new InvalidDataException("Missing property: 'text'");
        }
    }
}
