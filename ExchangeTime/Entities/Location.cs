using NodaTime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Media;

namespace ExchangeTime
{
    internal class Location
    {
        internal readonly string Name, Country, Region = "";
        internal readonly DateTimeZone TimeZone;
        internal readonly SolidColorBrush Brush;
        internal readonly List<Bar> Bars;
        internal readonly List<EarlyClose> EarlyCloses = new();
        internal readonly List<Notification> Notifications = new();
        internal Location(JsonElement json)
        {
            Name = json.GetProperty("name").GetString() ?? throw new InvalidDataException("Missing property: 'name'");

            try
            {
                Country = json.GetProperty("country").GetString() ?? throw new InvalidDataException("Missing property: 'country'");

                // optional
                if (json.TryGetProperty("region", out JsonElement r))
                    Region = r.GetString()!;

                string tz = json.GetProperty("timezone").GetString() ?? throw new InvalidDataException("Missing property: 'timezone'");
                TimeZone = Clock.DateTimeZoneProvider.GetZoneOrNull(tz) ?? throw new Exception($"Invalid timezone: {tz}.");

                // optional
                string color = "grey";
                if (json.TryGetProperty("color", out JsonElement colorJson))
                    color = colorJson.GetString() ?? color;

                Brush = MyBrushes.CreateBrush(color);

                Bars = json.GetProperty("bars").EnumerateArray().Select(interval => new Bar(interval)).ToList();

                // optional
                if (json.TryGetProperty("earlycloses", out JsonElement earlyCloses))
                {
                    foreach (JsonElement n in earlyCloses.EnumerateArray())
                        EarlyCloses.Add(new EarlyClose(n));
                }

                // optional
                if (json.TryGetProperty("notifications", out JsonElement notifications))
                {
                    foreach (JsonElement n in notifications.EnumerateArray())
                        Notifications.Add(new Notification(n));
                }
            }
            catch (Exception e)
            {
                throw new JsonException($"Error parsing json for location: {Name}.", e);
            }
        }
    }
}
