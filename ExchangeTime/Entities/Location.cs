using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Windows.Media;

//Interval: represents a specific time interval.

namespace ExchangeTime
{
    internal class Location
    {
        internal readonly string Name, Country, Region = "";
        internal readonly DateTimeZone TimeZone;
        internal readonly SolidColorBrush Brush;
        internal readonly List<Bar> Bars;
        internal readonly List<EarlyClose> EarlyCloses = new List<EarlyClose>();
        internal readonly List<Notification> Notifications = new List<Notification>();
        internal Location(JsonElement json)
        {
            Name = json.GetProperty("name").GetString();

            try
            {
                Country = json.GetProperty("country").GetString();

                // optional
                if (json.TryGetProperty("region", out var r))
                    Region = r.GetString();

                var tz = json.GetProperty("timezone").GetString();
                TimeZone = Clock.DateTimeZoneProvider.GetZoneOrNull(tz) ?? throw new Exception($"Invalid timezone: {tz}.");

                // optional
                var color = json.TryGetProperty("color", out var colorJson) ? colorJson.GetString() : "grey";
                Brush = MyBrushes.CreateBrush(color);

                Bars = json.GetProperty("bars").EnumerateArray().Select(interval => new Bar(interval)).ToList();

                // optional
                if (json.TryGetProperty("earlycloses", out var earlyCloses))
                {
                    foreach (var n in earlyCloses.EnumerateArray())
                        EarlyCloses.Add(new EarlyClose(n));
                }

                // optional
                if (json.TryGetProperty("notifications", out var notifications))
                {
                    foreach (var n in notifications.EnumerateArray())
                        Notifications.Add(new Notification(n, TimeZone));
                }
            }
            catch (Exception e)
            {
                throw new JsonException($"Error parsing json for location: {Name}.", e);
            }
        }
    }
}
