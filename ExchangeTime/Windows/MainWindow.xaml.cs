using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using HolidayService;
using System.Net;
using ExchangeTime.Utility;
using Microsoft.Extensions.Options;
using Jot;
using System.Threading;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
namespace ExchangeTime;

public sealed partial class MainWindow : Window, IDisposable
{
    private const int BarTop = 30;
    private const int BarHeight = 16;
    private const int Y1Hours = 15, Y1Ticks = 27;
    private readonly int width, height;
    private readonly DateTimeZone TimeZone = DateTimeZoneProviders.Bcl.GetSystemDefault();
    private readonly IClock Clock;
    private readonly ILogger Logger;
    private readonly IOptions<AppSettings> Settings;
    private readonly Holidays Holidays;
    private readonly AudioService AudioService;
    private readonly ZoomFormats zoomFormats = new();
    private readonly Canvas canvas = new();
    private readonly TextBlock centerHeader, leftHeader, rightHeader;
    private readonly List<Location> Locations;
    private readonly DispatcherTimer timer = new();
    private readonly CancellationTokenSource Cts = new();

    public MainWindow(IClock clock, ILogger<MainWindow> logger, IOptions<AppSettings> settings, Tracker tracker, Holidays holidays, AudioService audioService)
    {
        Clock = clock;
        Logger = logger;
        Settings = settings;
        Holidays = holidays;
        AudioService = audioService;

        InitializeComponent();

        // hardware pixels
        // DIP (device independent pixels) are 1/96 inch, virtiual
        //var xx = VisualTreeHelper.GetDpi(this).PixelsPerDip;

        FontSize = 12;
        Width = 649;

        ArgumentNullException.ThrowIfNull(tracker);
        tracker.Configure<MainWindow>()
             .Id(w => w.Name)
             .Properties(w => new { w.Left, w.Top, w.zoomFormats.Index })
             .PersistOn(nameof(Closing));
        tracker.Track(this);

        ArgumentNullException.ThrowIfNull(settings);
        Locations = [.. JsonDocument
            .Parse(File.ReadAllText(settings.Value.DataFilePath))
            .RootElement
            .GetProperty("locations")
            .EnumerateArray()
            .Select(location => new Location(location))];

        width = Convert.ToInt32(Width - 2 * BorderThickness.Left);
        Height = Locations.Count * BarHeight + 34;
        height = Convert.ToInt32(Height - 2 * BorderThickness.Top);

        leftHeader = CreateTopTextBlock(TextAlignment.Left);
        leftHeader.Text = " " + TimeZone.Id;
        centerHeader = CreateTopTextBlock(TextAlignment.Center);
        rightHeader = CreateTopTextBlock(TextAlignment.Right);

        AddChild(canvas);
        Repaint();

        TextBlock CreateTopTextBlock(TextAlignment ta) => new()
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
            await Holidays.LoadAllHolidays(Locations.Select(x => (x.Country, x.Region)), Cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            Logger.LogInformation("Cancelling.");
            return;
        }
        catch (WebException e)
        {
            HttpWebResponse response = (HttpWebResponse?)e.Response ?? throw new InvalidOperationException("No response object!");
            if (response.StatusCode != HttpStatusCode.TooManyRequests)
                throw;
            string msg = "Enrico Holiday Service: too many requests.";
            MessageBox.Show(msg, "warning", MessageBoxButton.OK);
            Logger.LogWarning(e, "{Message}", msg);
        }

        timer.Tick += Repaint;
        timer.Interval = TimeSpan.FromSeconds(1);
        timer.Start();
    }

    private async void Repaint(object? sender = null, EventArgs? e = null)
    {
        ZonedDateTime zdt = Clock.GetCurrentInstant().InZone(TimeZone);
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
        if (Settings.Value.AudioEnable)
            await Notify(instant).ConfigureAwait(false);
    }

    private void Window_Closing(object sender, CancelEventArgs e) => Cts.Cancel();
    public void Dispose() => Cts.Dispose();
}
