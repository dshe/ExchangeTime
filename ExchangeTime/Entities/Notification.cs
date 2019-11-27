using System.Text.Json;
using NodaTime;
using NodaTime.Text;

namespace ExchangeTime
{
    internal class Notification
    {
        private static readonly LocalTimePattern TimePattern = LocalTimePattern.CreateWithInvariantCulture("HH:mm:ss");
        internal readonly LocalTime Time;
        internal readonly string Text;
        internal Notification(JsonElement json, DateTimeZone timeZone)
        {
            var time = json.GetProperty("time").GetString();
            Time = TimePattern.Parse(time).Value;
            Text = json.GetProperty("text").GetString();
        }
    }
}
