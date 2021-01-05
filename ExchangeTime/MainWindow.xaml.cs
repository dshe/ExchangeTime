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

namespace ExchangeTime
{
    public partial class MainWindow : Window
    {
        private const string dataFileName = "data.json";
        private const int Y1Hours = 15, Y1Ticks = 27;
        private readonly Clock Clock;
        private readonly Holidays Holidays;
        private readonly int width, height;
        private readonly ZoomFormat zoomFormats;
		private readonly Canvas canvas = new Canvas();
        private readonly TextBlock centerHeader, leftHeader, rightHeader;
        private readonly List<Location> Locations;

        private readonly DispatcherTimer timer = new DispatcherTimer();
        private readonly Speech Speech = new Speech();

        public MainWindow()
		{
            Clock = new Clock();
            Holidays = new Holidays(Clock);

            InitializeComponent();

			if (Properties.Settings.Default.UpgradeSettings)
			{
				Properties.Settings.Default.Upgrade(); // retrieve the setting from the previous installation (if possible)
				//changing either the company or the applications name will prevent future upgrades(?)
				Properties.Settings.Default.UpgradeSettings = false; // this property; save will set the user property UpgradeSettings to false
				//Properties.Settings.Default.FilePath = Properties.Settings.Default.FilePath; // force save HolidaysFilePath setting to user.config (useful if no HolidaysFilePath not set by user yet)
				Properties.Settings.Default.Save(); // save user settings
			}

            using var stream = File.OpenRead(dataFileName);
            Locations = JsonDocument
                .Parse(stream)
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

            zoomFormats = new ZoomFormat(Properties.Settings.Default.ZoomLevel);
            AddChild(canvas);
            Repaint();
        }

        private TextBlock CreateTopTextBlock(TextAlignment ta)
        {
            return new TextBlock
            {
                Width = width,
                VerticalAlignment = VerticalAlignment.Top,
                FontSize = FontSize + 1,
                TextAlignment = ta,
                Foreground = MyBrushes.Gray224,
                Background = (ta == TextAlignment.Center) ? MyBrushes.Gray48 : Brushes.Transparent
            };
        }

        private async void Window_Loaded(object sender, RoutedEventArgs rea)
        {
            try
            {
                foreach (var location in Locations)
                    await Holidays.LoadHolidays(location.Country, location.Region);
            }
            catch (WebException e)
            {
                var response = (HttpWebResponse?)e.Response;
                if (response == null)
                    throw new Exception("No response object!");
                if (response.StatusCode != HttpStatusCode.TooManyRequests)
                    throw;
                MessageBox.Show("Enrico Holiday Service: too many requests.", "warning", MessageBoxButton.OK);
            }

            timer.Tick += Repaint;
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Start();
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			Properties.Settings.Default.ZoomLevel = zoomFormats.Index;
			Properties.Settings.Default.Save(); // save user settings (position)
        }

        private async void Repaint(object? sender = null, EventArgs? e = null)
        {
            var instant = Clock.GetCurrentInstant();
            var local = Clock.GetSystemZonedDateTime();

            centerHeader.Text = local.ToString("dddd, MMMM d, yyyy", null);
            rightHeader.Text = local.ToString("H:mm:ss ", null);

            canvas.Children.Clear();
            canvas.Children.Add(centerHeader);
            canvas.Children.Add(leftHeader);
            canvas.Children.Add(rightHeader);

            var originSeconds = instant.ToUnixTimeSeconds() - zoomFormats.SecondsPerPixel * width / 3;

            DrawTicks(originSeconds);
            DrawAllBars(originSeconds);
            DrawCursor();
            await Notify(instant);
        }

        private int DateToPixels(long originSeconds, ZonedDateTime dt)
        {
            var seconds = dt.ToInstant().ToUnixTimeSeconds();
            var px = (seconds - originSeconds) / zoomFormats.SecondsPerPixel;
            return Convert.ToInt32(px);
        }

        private async Task Notify(Instant instant)
        {
            if (!Properties.Settings.Default.Audio)
                return;

            foreach (var location in Locations.Where(loc => loc.Notifications.Any()))
            {
                var dt = instant.InZone(location.TimeZone).LocalDateTime;

                // don't notify on weekends
                if (dt.IsWeekend(location.Country))
                    continue;

                // don't notify on holidays
                var holiday = Holidays.TryGetHoliday(location.Country, location.Region, dt.Date);
                var earlyClose =  location.EarlyCloses.Where(x => x.DateTime.Date == dt.Date).SingleOrDefault();

                if (holiday != null && earlyClose == null)
                    continue;
                if (earlyClose != null && dt.TimeOfDay > earlyClose.DateTime.TimeOfDay)
                    continue;

                // holiday   earlyClose   Notify
                //  Y          N          No
                //  Y          Y          <= earlyClose
                //  N          Y          <= earlyClose
                //  N          N          Yes

                foreach (var notification in location.Notifications)
                {
                    if (dt.TimeOfDay == notification.Time)
                        await Speech.AnnounceTime(dt, location.Name, notification.Text).ConfigureAwait(false);
                }
            }
        }
    }
}
