using NodaTime;
using NodaTime.Text;
using System;
using System.IO;
using System.Text.Json;

namespace ExchangeTime
{
    internal class Bar
    {
        private static readonly LocalTimePattern TimePattern = LocalTimePattern.CreateWithInvariantCulture("HH:mm");
        internal readonly string Label = "";
        internal readonly LocalTime Start, End;
        internal readonly BarSize BarSize;
        internal Bar(JsonElement json)
        {
            string start = json.GetProperty("start").GetString() ?? throw new InvalidDataException("Missing property: 'start'");
            Start = TimePattern.Parse(start).Value;

            string end = json.GetProperty("end").GetString() ?? throw new InvalidDataException("Missing property: 'end'");
            End = TimePattern.Parse(end).Value;

            string type = json.GetProperty("type").GetString() ?? throw new InvalidDataException("Missing property: 'type'");
            BarSize = (BarSize)Enum.Parse(typeof(BarSize), type);

            bool hasLabel = json.TryGetProperty("label", out JsonElement label);
            if (BarSize == BarSize.L)
                Label = label.GetString() ?? throw new InvalidDataException("Missing property: 'label'");
            else if (hasLabel)
                throw new JsonException($"Bar label will not be displayed for BarHeight={BarSize}.");
        }
    }
}
