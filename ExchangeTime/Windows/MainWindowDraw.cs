using System.Linq;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using HolidayService;
using ExchangeTime.Utility;
using System.Threading.Tasks;

namespace ExchangeTime;

public sealed partial class MainWindow
{
    private void DrawTicks(long originSeconds)
    {
        long first = originSeconds - (originSeconds % zoomFormats.Minor) + zoomFormats.Minor;
        for (long s = first; s - first < width * zoomFormats.SecondsPerPixel; s += zoomFormats.Minor)
            DrawTick(originSeconds, s);

        void DrawTick(long originSeconds, long s)
        {
            long px = (s - originSeconds) / zoomFormats.SecondsPerPixel;
            if (s % zoomFormats.Major == 0)
                DrawMajorTick(px, s);
            else
                DrawMinorTick(px);

            void DrawMajorTick(long px, long s)
            {
                TextBlock tb = new()
                {
                    FontSize = FontSize,
                    Foreground = MyBrushes.Gray128,
                    Text = Instant.FromUnixTimeSeconds(s).InZone(TimeZone).ToString(zoomFormats.MajorFormat, null),
                    TextAlignment = TextAlignment.Center
                };
                Size size = tb.GetTextSize();
                tb.Width = size.Width + 15;
                tb.Height = size.Height;
                double halfWidth = tb.Width / 2.0;
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

            void DrawMinorTick(long px)
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
        }
    }

    private void DrawAllBars(long originSeconds)
    {
        Instant originInstant = Instant.FromUnixTimeSeconds(originSeconds);
        Instant endInstant = Instant.FromUnixTimeSeconds(originSeconds + zoomFormats.SecondsPerPixel * width);
        int y = BarTop;
        foreach (Location location in Locations)
        {
            DrawBarsForLocation(originSeconds, originInstant, endInstant, location, y);
            y += BarHeight;
        }
    }

    private void DrawBarsForLocation(long originSeconds, Instant originInstant, Instant endInstant, Location location, int y)
    {
        LocalDateTime dt1 = originInstant.InZone(location.TimeZone).Date.AtMidnight();
        LocalDateTime dt2 = endInstant.InZone(location.TimeZone).LocalDateTime;
        Debug.Assert(dt1 < dt2);

        if (dt1.DayOfWeekend(location.Country) == 2)
            DrawBar(originSeconds, location, dt1, dt1.PlusDays(1), BarSize.Weekend, "Weekend;W", y);

        for (LocalDateTime dt = dt1; dt < dt2; dt = dt.PlusDays(1))
        {
            int dayOfWeekend = dt.DayOfWeekend(location.Country);
            if (dayOfWeekend == 1)
                DrawBar(originSeconds, location, dt, dt.PlusDays(2), BarSize.Weekend, "Weekend;W", y);
            if (dayOfWeekend == 1 || dayOfWeekend == 2)
                continue;

            EarlyClose? earlyClose = location.EarlyCloses.Where(x => x.DateTime.Date == dt.Date).SingleOrDefault();
            if (earlyClose == null && Holidays.TryGetHoliday(location.Country, location.Region, dt.Date, out Holiday holiday))
            {
                DrawBar(originSeconds, location, dt, dt.PlusDays(1), BarSize.Holiday, "Holiday: " + holiday.Name + ";Holiday;H", y);
                continue;
            }

            foreach (Bar bar in location.Bars)
            {
                LocalDateTime start = dt.Date + bar.Start;
                LocalDateTime end = dt.Date + bar.End;
                if (earlyClose != null)
                {
                    if (earlyClose.DateTime.TimeOfDay <= bar.Start)
                        continue;
                    if (earlyClose.DateTime.TimeOfDay < bar.End)
                        end = earlyClose.DateTime;
                }
                string label = bar.Label;
                if (string.IsNullOrEmpty(label))
                    label = location.Name + " TIME";
                if (label.Contains("TIME", StringComparison.Ordinal))
                {
                    var timestring = Clock.GetCurrentInstant().InZone(location.TimeZone).ToString("H:mm", null);
                    label = label.Replace("TIME", timestring, StringComparison.Ordinal);
                }
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
        int x2 = DateToPixels(originSeconds, end.InZoneLeniently(location.TimeZone));

        if (x1 <= 0)
            x1 = 0;
        else if (x1 >= width)
            return;
        if (x2 >= width)
            x2 = width - 1;
        if (x2 < 0)
            return;

        TextBlock tb = new()
        {
            Foreground = MyBrushes.Gray224,
            Background = location.Brush,
            Opacity = .95,
            TextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            FontSize = FontSize,
            LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
            Width = x2 - x1 + 1
        };

        int y1 = top;

        switch (barHeight)
        {
            case BarSize.Holiday:
            case BarSize.Weekend:
                tb.Background = location.Brush.Clone();
                tb.Background.Opacity = .5;
                tb.Foreground = MyBrushes.Gray224;
                tb.Height = BarHeight; 
                tb.Text = label;
                tb.FitText();
                break;
            case BarSize.L:
                tb.Height = BarHeight;
                if (zoomFormats.SecondsPerPixel < 1800)
                {
                    tb.Text = label;
                    tb.FitText();
                }
                break;
            case BarSize.M:
                tb.Height = Convert.ToInt32(BarHeight / 2.0);
                //y1 += 3;
                y1 += Convert.ToInt32(BarHeight/2.0 - BarHeight / 4.0);
                break;
            case BarSize.S:
                tb.Height = Convert.ToInt32(BarHeight / 4.0);
                //y1 += 5;
                y1 += Convert.ToInt32(BarHeight / 2.0 - BarHeight / 8.0);
                break;
        }
        Canvas.SetTop(tb, y1);
        Canvas.SetLeft(tb, x1);
        canvas.Children.Add(tb);

        int DateToPixels(long originSeconds, ZonedDateTime dt)
        {
            long seconds = dt.ToInstant().ToUnixTimeSeconds();
            long px = (seconds - originSeconds) / zoomFormats.SecondsPerPixel;
            return Convert.ToInt32(px);
        }
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

    private async Task Notify(Instant instant)
    {
        foreach (Location location in Locations.Where(loc => loc.Notifications.Any()))
        {
            LocalDateTime dt = instant.InZone(location.TimeZone).LocalDateTime;

            // don't notify on weekends
            if (dt.IsWeekend(location.Country))
                continue;

            // don't notify on holidays
            EarlyClose? earlyClose = location.EarlyCloses.Where(x => x.DateTime.Date == dt.Date).SingleOrDefault();
            if (Holidays.TryGetHoliday(location.Country, location.Region, dt.Date, out Holiday holiday) && earlyClose == null)
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
                if (dt.TimeOfDay != notification.Time)
                    continue;
                await AudioService.AnnounceTime(dt, location.Name, notification.Text).ConfigureAwait(false);
            }
        }
    }
}
