using NodaTime.Text;
using System.IO;
using System.Text.Json;

namespace ExchangeTime;

internal readonly struct EarlyClose
{
    private static readonly LocalDateTimePattern DateTimePattern = LocalDateTimePattern.CreateWithInvariantCulture("yyyy-MM-ddTHH:mm");
    internal static EarlyClose Undefined { get; }
    internal LocalDateTime DateTime { get; }
    internal string Name { get; }
    internal bool IsValid => DateTime != default;
    public EarlyClose()
    {
        DateTime = default;
        Name = "";
    }
    internal EarlyClose(JsonElement json)
    {
        string date = json.GetProperty("date").GetString() ?? throw new InvalidDataException("Missing property: 'date'");
        DateTime = DateTimePattern.Parse(date).Value;
        Name = json.GetProperty("name").GetString() ??
            throw new InvalidDataException("Missing property: 'name'");
    }
}
