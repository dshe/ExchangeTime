using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using NodaTime;
using HolidayService;
using SpeechService;
using System.Net;
using ExchangeTime.Utility;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Jot;

namespace ExchangeTime.Windows
{
    public partial class MainWindow : Window
    {
        private readonly IOptions<AppSettings> Settings;
        private const string dataFileName = "data.json";
        private const int Y1Hours = 15, Y1Ticks = 27;
        private readonly Clock Clock;
        private readonly Holidays Holidays;
        private readonly int width, height;
        private readonly ZoomFormat zoomFormats = new ZoomFormat();
		private readonly Canvas canvas = new();
        private readonly TextBlock centerHeader, leftHeader, rightHeader;
        private readonly List<Location> Locations;
        private readonly DispatcherTimer timer = new();
        private readonly Speech Speech;

        public MainWindow(ILogger<MainWindow> logger, IOptions<AppSettings> settings, Tracker tracker, Clock clock, Holidays holidays, Speech speech)
		{
            Settings = settings;
            Clock = clock;
            Holidays = holidays;
            Speech = speech;

            tracker.Configure<MainWindow>()
                 .Id(w => w.Name)
                 .Properties(w => new { w.Left, w.Top, w.zoomFormats.Index })
                 .PersistOn(nameof(Closing));
            tracker.Track(this);

            InitializeComponent();

            Locations = JsonDocument
                .Parse(File.ReadAllText(dataFileName))
                .RootElement
                .GetProperty("locations")
                .EnumerateArray()
                .Select(location => new Location(location))
                .ToList();

            width = Convert.ToInt32(Width - 2 * BorderThickness.Left); // 484 -> 480
            Height = Locations.Count * 11 + 32;
            height = Convert.ToInt32(Height - 2 * BorderThickness.Top);

            centerHeader = CreateTopTextBlock(TextAlignment.Center);
            rightHeader = CreateTopTextBlock(TextAlignment.Right);
            leftHeader = CreateTopTextBlock(TextAlignment.Left);
            leftHeader.Text = " " + Clock.SystemTimeZone.Id;

            AddChild(canvas);
            Repaint();
        }

        private TextBlock CreateTopTextBlock(TextAlignment ta) => new()
        {
            Width = width,
            VerticalAlignment = VerticalAlignment.Top,
            FontSize = FontSize + 1,
            TextAlignment = ta,
            Foreground = MyBrushes.Gray224,
            Background = (ta == TextAlignment.Center) ? MyBrushes.Gray48 : Brushes.Transparent
        };

        private async void Window_Loaded(object sender, RoutedEventArgs rea)
        {
            try
            {
                foreach (Location location in Locations)
                    await Holidays.LoadHolidays(location.Country, location.Region);
            }
            catch (WebException e)
            {
                HttpWebResponse response = (HttpWebResponse?)e.Response ?? throw new Exception("No response object!");
                if (response.StatusCode != HttpStatusCode.TooManyRequests)
                    throw;
                MessageBox.Show("Enrico Holiday Service: too many requests.", "warning", MessageBoxButton.OK);
            }

            timer.Tick += Repaint;
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Start();
        }

        private async void Repaint(object? sender = null, EventArgs? e = null)
        {
            ZonedDateTime zdt = Clock.GetSystemZonedDateTime();

            centerHeader.Text = zdt.ToString("dddd, MMMM d, yyyy", null);
            rightHeader.Text = zdt.ToString("H:mm:ss ", null);

            canvas.Children.Clear();
            canvas.Children.Add(centerHeader);
            canvas.Children.Add(leftHeader);
            canvas.Children.Add(rightHeader);

            Instant instant = zdt.ToInstant();

            long originSeconds = instant.ToUnixTimeSeconds() - zoomFormats.SecondsPerPixel * width / 3;
            DrawTicks(originSeconds);
            DrawAllBars(originSeconds);
            DrawCursor();
            await Notify(instant);
        }

        private int DateToPixels(long originSeconds, ZonedDateTime dt)
        {
            long seconds = dt.ToInstant().ToUnixTimeSeconds();
            long px = (seconds - originSeconds) / zoomFormats.SecondsPerPixel;
            return Convert.ToInt32(px);
        }

        private async Task Notify(Instant instant)
        {
            if (!Settings.Value.AudioEnable)
                 return;

            foreach (Location location in Locations.Where(loc => loc.Notifications.Any()))
            {
                LocalDateTime dt = instant.InZone(location.TimeZone).LocalDateTime;

                // don't notify on weekends
                if (dt.IsWeekend(location.Country))
                    continue;

                // don't notify on holidays
                Holiday? holiday = Holidays.TryGetHoliday(location.Country, location.Region, dt.Date);
                EarlyClose? earlyClose =  location.EarlyCloses.Where(x => x.DateTime.Date == dt.Date).SingleOrDefault();

                if (holiday != null && earlyClose == null)
                    continue;
                if (earlyClose != null && dt.TimeOfDay > earlyClose.DateTime.TimeOfDay)
                    continue;

                // holiday   earlyClose   Notify
                //  Y          N          No
                //  Y          Y          <= earlyClose
                //  N          Y          <= earlyClose
                //  N          N          Yes

                foreach (Notification notification in location.Notifications)
                {
                    if (dt.TimeOfDay == notification.Time)
                        await Speech.AnnounceTime(dt, location.Name, notification.Text).ConfigureAwait(false);
                }
            }
        }
    }
}
