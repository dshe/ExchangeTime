using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Media;
using System.Xml;
using NodaTime;
using NodaTime.Text;

namespace ExchangeTime.Code
{
    internal enum BarHeight
    {
        L,
        M,
        S,
        Holiday,
        Weekend
    };

    public class Holiday
    {
        internal readonly string Name;
        internal readonly LocalDate Date;
        internal readonly LocalTime EarlyClose;

        internal Holiday(XmlNode node)
        {
            var attributes = node.Attributes;
            if (attributes == null)
                throw new Exception("Holiday node has no attributes.");

            var a = attributes["Name"];
            if (a == null)
                throw new Exception("Holiday node has no Name attribute.");
            Name = attributes["Name"].Value;

            a = attributes["Date"];
            if (a == null)
                throw new Exception("Holiday node has no Date attribute.");
            Date = LocalDatePattern.Iso.Parse(a.Value).Value;

            a = attributes["EarlyClose"]; // optional
            if (a != null)
                EarlyClose = LocalTimePattern.ExtendedIso.Parse(a.Value).Value;
        }
    }

    public class Interval
    {
        internal readonly string Label;
        internal readonly LocalTime Start, End;
        internal readonly int Shift;
        internal readonly BarHeight BarHeight;
        public Interval(XmlNode node)
        {
            var attributes = node.Attributes;
            if (attributes == null)
                throw new Exception("Interval node has no attributes.");
            Label = attributes["Label"].Value; // optional
            Start = LocalTimePattern.ExtendedIso.Parse(attributes["Start"].Value).Value;
            End = LocalTimePattern.ExtendedIso.Parse(attributes["End"].Value).Value; // required

            var a = attributes["Shift"]; // optional
            if (a != null)
                Shift = int.Parse(a.Value);

            a = attributes["BarHeight"]; // required
            Trace.Assert(Enum.TryParse(a.Value, out BarHeight));
        }
    }

    public class Notification
    {
        internal readonly LocalTime Time;
        internal readonly string Text;
        internal Notification(XmlNode node)
        {
            var attributes = node.Attributes;
            if (attributes == null)
                throw new Exception("Notification node has no attributes.");
            Time = LocalTimePattern.ExtendedIso.Parse(attributes["Time"].Value).Value;
            Text = attributes["Text"].Value;
        }
    }

    public class Location
    {
        internal readonly string Name;
        internal readonly Brush Brush;
        internal readonly DateTimeZone Tz;

        internal readonly List<Interval> Intervals = new List<Interval>();
        internal readonly List<Notification> Notifications = new List<Notification>();
        internal readonly Dictionary<LocalDate, Holiday> Holidays = new Dictionary<LocalDate, Holiday>();
        private static readonly BrushConverter BrushConverter = new BrushConverter();
        internal Location(XmlNode node)
        {
            var attributes = node.Attributes ?? throw new Exception("Location node has no attributes.");
            var a = attributes["Name"] ?? throw new Exception("Location node has no name attribute.");
            Name = a.Value;
            a = attributes["TimeZone"] ?? throw new Exception("Location node has No TimeZone attribute.");

            Tz = DateTimeZoneProviders.Tzdb.GetZoneOrNull(a.Value);
            if (Tz == null)
                throw new Exception("Could not find timezone for: " + a.Value);

            a = attributes["Color"]; // optional

            if (a != null)
            {
                try
                {
                    Brush = (SolidColorBrush)BrushConverter.ConvertFromString(a.Value);
                }
                catch
                {
                    throw new Exception("invalid color: " + a.Value);
                }
            }

            AddIntervals(node);
            AddNotifications(node);
            AddHolidays(node);
        }

        private void AddIntervals(XmlNode node)
        {
            var nodes = node.SelectNodes("Interval"); // require at least one
            if (nodes == null || nodes.Count == 0)
                throw new Exception("No 'Interval' nodes found location '" + Name + "'.");
            foreach (XmlNode n in nodes)
                Intervals.Add(new Interval(n));
        }

        private void AddNotifications(XmlNode node)
        {
            foreach (XmlNode n in node.SelectNodes("Notification"))
                Notifications.Add(new Notification(n));
        }

        private void AddHolidays(XmlNode node)
        {
            foreach (XmlNode n in node.SelectNodes("Holiday"))
            {
                var holiday = new Holiday(n);
                Holidays.Add(holiday.Date, holiday);
            }
        }
    }
}
