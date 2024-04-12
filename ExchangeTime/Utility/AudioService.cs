using Microsoft.Extensions.Logging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Media;
using System.Threading.Tasks;
using System.Speech.Synthesis;
using System.Threading;

namespace ExchangeTime.Utility;

public sealed class AudioService : IDisposable
{
    private readonly ILogger Logger;
    private readonly SoundPlayer SoundPlayer = new();
    private readonly SpeechSynthesizer SpeechSynthesizer = new();
    private readonly SemaphoreSlim Semaphore = new(0, 1);

    public AudioService(ILogger<AudioService> logger)
    {
        Logger = logger;

        var cultureInfo = CultureInfo.CreateSpecificCulture("en-US");
        if (SpeechSynthesizer.GetInstalledVoices(cultureInfo).Any(v => v.VoiceInfo.Name == "Microsoft David Desktop"))
            SpeechSynthesizer.SelectVoice("Microsoft David Desktop");

        SpeechSynthesizer.SpeakCompleted += (object? sender, SpeakCompletedEventArgs e) =>  Semaphore.Release();
    }

    private async Task PlayWindowsMediaFile(string fileName = "Alarm01.wav")
    {
        ArgumentNullException.ThrowIfNull(fileName);

        string windir = Environment.GetEnvironmentVariable("windir") ?? throw new InvalidOperationException("Windows directory not found.");
        string path = $"{windir}\\Media\\{fileName}";
        await PlayAudioFileAsync(path).ConfigureAwait(false);
    }

    private async Task PlayAudioFileAsync(string fileName)
    {
        Logger.LogInformation("Playing audio file: {FileName}.", fileName);
        if (!File.Exists(fileName))
            throw new FileNotFoundException(fileName);

        SoundPlayer.SoundLocation = fileName;

        await Task.Run(SoundPlayer.PlaySync).ConfigureAwait(false);
    }

    public async Task AnnounceTime(ZonedDateTime zdt, string text = "")
    {
        string city = zdt.Zone.Id;
        if (city.Contains('/', StringComparison.Ordinal))
            city = city.Split("/")[1];
        await AnnounceTime(zdt.LocalDateTime, city, text).ConfigureAwait(false);
    }

    public async Task AnnounceTime(LocalDateTime dt, string locationName = "", string text = "")
    {
        await PlayWindowsMediaFile().ConfigureAwait(false);

        StringBuilder sb = new($"It is now {dt.Date.ToString("MMMM d", null)} at {dt.ToString("H:mm", null)}");
        if (!string.IsNullOrWhiteSpace(locationName))
            sb.Append(CultureInfo.InvariantCulture, $" in {locationName}");
        sb.Append(". ");
        if (!string.IsNullOrWhiteSpace(text))
            sb.Append(text + ".");
        string txt = sb.ToString();
        await SpeakAsync(txt).ConfigureAwait(false);
    }

    private async Task SpeakAsync(string text)
    {
        Logger.LogInformation("Speaking: {Text}", text);
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("No text to speak.");

        SpeechSynthesizer.SpeakAsync(text);

        await Semaphore.WaitAsync().ConfigureAwait(false);
    }

    public void Dispose()
    {
        SpeechSynthesizer.Dispose();
        SoundPlayer.Dispose();
        Semaphore.Dispose();
    }
}
