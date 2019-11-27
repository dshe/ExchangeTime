using NodaTime;
using NodaTime.Text;
using System;
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
            var start = json.GetProperty("start").GetString();
            Start = TimePattern.Parse(start).Value;

            var end = json.GetProperty("end").GetString();
            End = TimePattern.Parse(end).Value;

            var type = json.GetProperty("type").GetString();
            BarSize = (BarSize)Enum.Parse(typeof(BarSize), type);

            var hasLabel = json.TryGetProperty("label", out var label);
            if (BarSize == BarSize.L)
                Label = label.GetString();
            else if (hasLabel)
                throw new JsonException($"Bar label will not be displayed for BarHeight={BarSize}.");
        }
    }
}
