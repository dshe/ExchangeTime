using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using NodaTime;


namespace ExchangeTime
{
    public partial class MainWindow : Window
    {
        private void DrawTicks(long originSeconds)
        {
            long first = originSeconds - (originSeconds % zoomFormats.Minor) + zoomFormats.Minor;
            for (long s = first; s - first < width * zoomFormats.SecondsPerPixel; s += zoomFormats.Minor)
                DrawTick(originSeconds, s);
        }

        private void DrawTick(long originSeconds, long s)
        {
            var px = (s - originSeconds) / zoomFormats.SecondsPerPixel;
           if (s % zoomFormats.Major == 0)
                DrawMajorTick(px, s);
            else
                DrawMinorTick(px);
        }

        private void DrawMajorTick(long px, long s)
        {
            var tb = new TextBlock
            {
                Foreground = MyBrushes.Gray128,
                Text = Instant.FromUnixTimeSeconds(s).InZone(Clock.SystemTimeZone).ToString(zoomFormats.MajorFormat, null),
                TextAlignment = TextAlignment.Center
            };
            var size = tb.GetTextSize();
            tb.Width = size.Width + 4;
            tb.Height = size.Height;
            var halfWidth = tb.Width / 2;
            if (px >= halfWidth && width - px >= halfWidth)
            {
                Canvas.SetTop(tb, Y1Hours);
                Canvas.SetLeft(tb, px - halfWidth);
                canvas.Children.Add(tb);
            }
            canvas.Children.Add(new Line
            {
                X1 = px,
                X2 = px,
                Y1 = Y1Ticks, // top + text height
                Y2 = height,  // straight line
                Stroke = MyBrushes.Gray96,
                StrokeThickness = 1
            });
        }

        private void DrawMinorTick(long px)
        {
            canvas.Children.Add(new Line
            {
                X1 = px,
                X2 = px,
                Y1 = Y1Ticks + 2,
                Y2 = height,
                StrokeThickness = 1,
                Stroke = MyBrushes.Gray48
            });
        }

        private void DrawAllBars(long originSeconds)
        {
            var originInstant = Instant.FromUnixTimeSeconds(originSeconds);
            var endInstant = Instant.FromUnixTimeSeconds(originSeconds + zoomFormats.SecondsPerPixel * width);
            int y = 28;
            foreach (var location in Locations)
            {
                DrawBarsForLocation(originSeconds, originInstant, endInstant, location, y);
                y += 11;
            }
        }

        private void DrawBarsForLocation(long originSeconds, Instant originInstant, Instant endInstant,  Location location, int y)
        {
            var dt1 = originInstant.InZone(location.TimeZone).Date.AtMidnight();
            var dt2 = endInstant.InZone(location.TimeZone).LocalDateTime;
            Debug.Assert(dt1 < dt2);

            if (dt1.DayOfWeekend(location.Country) == 2)
                DrawBar(originSeconds, location, dt1, dt1.PlusDays(1), BarSize.Weekend, "Weekend;W", y);

            for (var dt = dt1; dt < dt2; dt = dt.PlusDays(1))
            {
                switch (dt.DayOfWeekend(location.Country))
                {
                    case 1:
                        DrawBar(originSeconds, location, dt, dt.PlusDays(2), BarSize.Weekend, "Weekend;W", y);
                        continue;
                    case 2: // skip, 2 days covers the weekend
                        continue;
                }

                var holiday = Holidays.TryGetHoliday(location.Country, location.Region, dt.Date);
                var earlyClose = location.EarlyCloses.Where(x => x.DateTime.Date == dt.Date).SingleOrDefault();

                if (holiday != null && earlyClose == null)
                {
                    DrawBar(originSeconds, location, dt, dt.PlusDays(1), BarSize.Holiday, "Holiday: " + holiday.Name + ";Holiday;H", y);
                    continue;
                }

                foreach (var bar in location.Bars)
                {
                    var start = dt.Date + bar.Start;
                    var end = dt.Date + bar.End;
                    if (earlyClose != null)
                    {
                        if (earlyClose.DateTime.TimeOfDay <= bar.Start)
                            continue;
                        if (earlyClose.DateTime.TimeOfDay < bar.End)
                            end = earlyClose.DateTime;
                    }
                    var label = bar.Label;
                    if (string.IsNullOrEmpty(label))
                        label = location.Name + " TIME";
                    if (label.Contains("TIME"))
                        label = label.Replace("TIME", Clock.GetCurrentInstant().InZone(location.TimeZone).ToString("H:mm", null));
                    DrawBar(originSeconds, location, start, end, bar.BarSize, label, y);
                }
            }
        }

        private void DrawBar(long originSeconds, Location location, LocalDateTime start, LocalDateTime end, BarSize barHeight, string label, int top)
        {
            if (start >= end)
                end = end.PlusDays(1);
            if (start >= end)
                return;

            int x1 = DateToPixels(originSeconds, start.InZoneLeniently(location.TimeZone));
            int x2 = DateToPixels(originSeconds,   end.InZoneLeniently(location.TimeZone));

            if (x1 <= 0)
                x1 = 0;
            else if (x1 >= width)
                return;
            if (x2 >= width)
                x2 = width - 1;
            if (x2 < 0)
                return;

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

            var y1 = top;

            switch (barHeight)
            {
                case BarSize.Holiday:
                case BarSize.Weekend:
                    tb.Background = location.Brush.Clone();
                    tb.Background.Opacity = .5;
                    tb.Foreground = MyBrushes.Gray224;
                    tb.Height = 11;
                    tb.Text = label;
                    tb.FitText();
                    break;
                case BarSize.L:
                    tb.Height = 11;
                    if (zoomFormats.SecondsPerPixel < 1800)
                    {
                        tb.Text = label;
                        tb.FitText();
                    }
                    break;
                case BarSize.M:
                    tb.Height = 5;
                    y1 += 3;
                    break;
                case BarSize.S:
                    tb.Height = 1;
                    y1 += 5;
                    break;
            }
            Canvas.SetTop(tb, y1);
            Canvas.SetLeft(tb, x1);
            canvas.Children.Add(tb);
        }

        private void DrawCursor()
        {   // show the line showing the current point in time
            canvas.Children.Add(new Line // vertical line 1 px width
            {
                X1 = width / 3,
                X2 = width / 3,
                Y1 = Y1Hours + 12,
                Y2 = height,
                Stroke = Brushes.Gold,
                StrokeThickness = 1
            });
        }
    }
}
