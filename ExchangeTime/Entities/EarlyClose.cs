using NodaTime;
using NodaTime.Text;
using System.IO;
using System.Text.Json;

namespace ExchangeTime
{
    internal class EarlyClose
    {
        private static readonly LocalDateTimePattern DateTimePattern = LocalDateTimePattern.CreateWithInvariantCulture("yyyy-MM-ddTHH:mm");
        internal readonly LocalDateTime DateTime;
        internal readonly string Name;
        internal EarlyClose(JsonElement json)
        {
            var date = json.GetProperty("date").GetString();
            if (date == null)
                throw new InvalidDataException("Missing property: 'date'");
            DateTime = DateTimePattern.Parse(date).Value;
            Name = json.GetProperty("name").GetString() ??
                throw new InvalidDataException("Missing property: 'name'");
        }
    }
}
