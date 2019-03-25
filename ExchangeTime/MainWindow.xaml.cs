using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using ExchangeTime.Code;
using ExchangeTime.Utility;
using NodaTime;

#nullable enable

namespace ExchangeTime
{
    public partial class MainWindow
	{
        private const int Y1Hours = 15, Y1Ticks = 27;
        private readonly int width, height;
        private readonly Formats formats = new Formats();
		private readonly DispatcherTimer timer = new DispatcherTimer();
		private readonly Canvas canvas1 = new Canvas();
        private readonly TextBlock centerHeader, leftHeader, rightHeader;
	    private readonly Speech speech = new Speech();
	    private readonly List<Location> locations = Data.GetLocations();
        private int formatIndex;
        private Format Format => formats[formatIndex];
        private int SecondsPerPixel => Format.SecondsPerPixel;
        private long OriginSeconds;

        public MainWindow()
		{
            InitializeComponent();
			if (Properties.Settings.Default.UpgradeSettings)
			{
				Properties.Settings.Default.Upgrade(); // retrieve the setting from the previous installation (if possible)
				//changing either the company or the applications name will prevent future upgrades(?)
				Properties.Settings.Default.UpgradeSettings = false; // this property; save will set the user property UpgradeSettings to false
				//Properties.Settings.Default.FilePath = Properties.Settings.Default.FilePath; // force save HolidaysFilePath setting to user.config (useful if no HolidaysFilePath not set by user yet)
				Properties.Settings.Default.Save(); // save user settings
			}
            formatIndex = Properties.Settings.Default.ZoomLevel;

            width = Convert.ToInt32(Width - 2 * BorderThickness.Left); // 484 -> 480
            Height = locations.Count * 11 + 32;
            height = Convert.ToInt32(Height - 2 * BorderThickness.Top);

            AddChild(canvas1);
            centerHeader = CreateTopTextBlock(TextAlignment.Center);
            leftHeader = CreateTopTextBlock(TextAlignment.Left);
            rightHeader = CreateTopTextBlock(TextAlignment.Right);

            timer.Tick += Tick;
            Tick(null, null);
        }

	    private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			Properties.Settings.Default.ZoomLevel = formatIndex;
			Properties.Settings.Default.Save(); // save user settings (position)
            speech.Dispose(); // automatically disposed when the process ends
        }

		private void Tick(object? sender, EventArgs? e)
		{
            timer.Stop();
            Repaint();
            timer.Interval = TimeSpan.FromSeconds(1) - TimeSpan.FromTicks(Clock.CurrentInstant.InUtc().TickOfSecond);
            timer.Start();
		}

        private void Repaint()
        {
            var instant = Clock.CurrentInstant.Round(NodaConstants.TicksPerSecond);
            var local = instant.InZone(Clock.SystemTimeZone);

            leftHeader.Text = " " + Clock.SystemTimeZone.Id;
            centerHeader.Text = local.ToString("dddd, MMMM d, yyyy", null);
            rightHeader.Text = local.ToString("H:mm:ss ", null);

            canvas1.Children.Clear();
            canvas1.Children.Add(centerHeader);
            canvas1.Children.Add(leftHeader);
            canvas1.Children.Add(rightHeader);

            OriginSeconds = instant.ToUnixTimeSeconds() - SecondsPerPixel * width / 3;

            DrawTicks();
            DrawBars();
            DrawCursor();
            Notify(instant);
        }

        private void DrawTicks()
        {
            long first = OriginSeconds - (OriginSeconds % Format.Minor) + Format.Minor;
            for (long s = first; s - first < width * Format.SecondsPerPixel; s += Format.Minor)
            {
                var instant = Instant.FromUnixTimeSeconds(s);
                ZonedDateTime dt = instant.InZone(Clock.SystemTimeZone);
                var i = (s - OriginSeconds) / Format.SecondsPerPixel;

                if (s % Format.Major == 0 && (Format.SecondsPerPixel < 3600 || dt.DayOfWeek == IsoDayOfWeek.Monday))
                {
                    var tb = new TextBlock
                    {
                        Foreground = MyBrushes.Gray128,
                        Text = dt.ToString(Format.MajorFormat, null),
                        TextAlignment = TextAlignment.Center
                    };
                    var size = tb.GetTextSize();
                    tb.Width = size.Width + 4;
                    tb.Height = size.Height;
                    var halfWidth = tb.Width / 2;
                    if (i >= halfWidth && width - i >= halfWidth)
                    {
                        Canvas.SetTop(tb, Y1Hours);
                        Canvas.SetLeft(tb, i - halfWidth);
                        canvas1.Children.Add(tb);
                    }
                    canvas1.Children.Add(new Line
                    {
                        X1 = i,
                        X2 = i,
                        Y1 = Y1Ticks, // top + text height
                        Y2 = height,  // straight line
                        Stroke = MyBrushes.Gray96,
                        StrokeThickness = 1
                    });
                }
                else
                {
                    canvas1.Children.Add(new Line
                    {
                        X1 = i,
                        X2 = i,
                        Y1 = Y1Ticks + 2,
                        Y2 = height,
                        StrokeThickness = 1,
                        Stroke = MyBrushes.Gray48
                    });
                }
            }
        }

        private void DrawBars()
        {
            int y = 28;
            var originInstant = Instant.FromUnixTimeSeconds(OriginSeconds);
            var endInstant = Instant.FromUnixTimeSeconds(OriginSeconds + SecondsPerPixel * 480);

            foreach (var location in locations)
            {
                var zoneOriginDate = originInstant.InZone(location.Tz);
                var zoneEndDate = endInstant.InZone(location.Tz);

                var dt1 = zoneOriginDate.Date.AtMidnight();
                var dt2 = zoneEndDate.LocalDateTime;
                Debug.Assert(dt1 < dt2);

                if (dt1.DayOfWeek == IsoDayOfWeek.Sunday)
                    DrawBar(location, dt1, dt1.PlusDays(1), BarHeight.Weekend, "Sunday;Sun;S", y);
                for (var dt = dt1; dt < dt2; dt = dt.PlusDays(1))
                {
                    if (dt.DayOfWeek == IsoDayOfWeek.Saturday)
                        DrawBar(location, dt, dt.PlusDays(2), BarHeight.Weekend, "Weekend;W", y);
                    if (dt.DayOfWeek == IsoDayOfWeek.Saturday || dt.DayOfWeek == IsoDayOfWeek.Sunday) // skip Sundays; 2 days from Saturday covers the Weekend
                        continue;
                    location.Holidays.TryGetValue(dt.Date, out Holiday holiday); // dt.Date has offset removed
                    var earlyClose = holiday == null ? default : holiday.EarlyClose;
                    if (holiday != null)
                    {
                        DrawBar(location, dt.Date + earlyClose, dt.PlusDays(1), BarHeight.Holiday, "Holiday: " + holiday.Name + ";Holiday;H", y);
                        if (earlyClose == default)
                            continue;
                    }
                    foreach (var interval in location.Intervals)
                    {
                        var start = dt.Date + interval.Start;
                        var end = dt.Date + (earlyClose == default ? interval.End : earlyClose);
                        if (earlyClose == default)
                        {
                            if (interval.Shift == 1)
                                end = end.PlusDays(1);
                            else if (interval.Shift == -1)
                                start = start.PlusDays(1);
                        }
                        var label = interval.Label;
                        if (string.IsNullOrEmpty(label))
                            label = location.Name + " TIME";
                        if (label.Contains("TIME"))
                            label = label.Replace("TIME", Clock.CurrentInstant.InZone(location.Tz).ToString("H:mm", null));
                        DrawBar(location, start, end, interval.BarHeight, label, y);
                    }
                }
                y += 11;
            }
        }

	    private void DrawBar(Location location, LocalDateTime start, LocalDateTime end, BarHeight barHeight, string label, int top)
		{
            if (start >= end)
                end = end.PlusDays(1);

            if (start >= end)
                return;

            var zone = location.Tz;
            int x1 = DateToPixels(zone.AtLeniently(start)), x2 = DateToPixels(zone.AtLeniently(end));

            if (x1 <= 0)
                x1 = 0;
            else if (x1 >= width)
                return;
            if (x2 >= width)
                x2 = width - 1;
            if (x2 < 0)
                return;

            var y1 = top;

		    var tb = new TextBlock
		    {
                Foreground = MyBrushes.Gray224,
                Background = location.Brush,
		        Opacity = .95,
		        TextAlignment = TextAlignment.Center,
		        LineHeight = 11,
		        LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
                Width = x2 - x1 + 1
            };

		    switch (barHeight)
		    {
		        case BarHeight.Holiday:
		        case BarHeight.Weekend:
                    tb.Background = location.Brush.Clone();
                    tb.Background.Opacity = .5;
		            tb.Foreground = MyBrushes.Gray224;
		            tb.Height = 11;
		            tb.Text = label;
		            tb.FitText();
		            break;
		        case BarHeight.L:
		            tb.Height = 11;
		            if (SecondsPerPixel < 1800)
		            {
		                tb.Text = label;
		                tb.FitText();
		            }
		            break;
		        case BarHeight.M:
		            tb.Height = 5;
		            y1 += 3;
		            break;
		        case BarHeight.S:
		            tb.Height = 1;
		            y1 += 5;
		            break;
		    }
			Canvas.SetTop(tb, y1);
			Canvas.SetLeft(tb, x1);
			canvas1.Children.Add(tb);
		}

        private void DrawCursor()
        {   // show the line showing the current point in time
            canvas1.Children.Add(new Line // vertical line 1 px width
            {
                X1 = width / 3,
                X2 = width / 3,
                Y1 = Y1Hours + 12,
                Y2 = height,
                Stroke = Brushes.Gold,
                StrokeThickness = 1
            });
        }

        private void Notify(Instant instant)
        {
            foreach (var location in locations.Where(loc => loc.Notifications.Any()))
            {
                var dt = instant.InZone(location.Tz);
                if (dt.DayOfWeek != IsoDayOfWeek.Saturday && dt.DayOfWeek != IsoDayOfWeek.Sunday && !location.Holidays.ContainsKey(dt.Date))
                {   // don't notify on weekends or holidays
                    foreach (var notification in location.Notifications)
                    {
                        if (dt.TimeOfDay == notification.Time && Properties.Settings.Default.Audio)
                            speech.AnnounceTime(dt, location.Name, notification.Text);
                    }
                }
            }
        }

        private int DateToPixels(ZonedDateTime dt)
        {
            var seconds = dt.ToInstant().ToUnixTimeSeconds();
            var px = (seconds - OriginSeconds) / SecondsPerPixel;
            return Convert.ToInt32(px);
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
	}
}
