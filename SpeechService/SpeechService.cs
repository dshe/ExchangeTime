using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using NodaTime;
using Google.Cloud.TextToSpeech.V1;
using NetCoreAudio;
using Microsoft.Extensions.Logging;
/*
Google Cloud Text-to-Speech API. The SDK must be installed.
https://cloud.google.com/text-to-speech/
Google services require registration! Monthly free tier: < 4 million characters.
Use "Service Account Key". IAM and Admin => service accounts for project
Download a "Service Account JSON file" and then set the environment variable to refer to it.
"GOOGLE_APPLICATION_CREDENTIALS"
*/

namespace SpeechService
{
    public class Speech
    {
        private readonly ILogger Logger;
        private readonly SemaphoreSlim TextToSpeechSemaphore = new(1);
        private readonly VoiceSelectionParams Voice = new() { LanguageCode = "en-US", SsmlGender = SsmlVoiceGender.Neutral };
        private readonly Lazy<TextToSpeechClient> Client = new(() => TextToSpeechClient.Create());

        public Speech(ILogger<Speech> logger) => Logger = logger;

        public async Task PlayAudioFile(string fileName)
        {
            Logger.LogInformation($"Playing audio file: {fileName}.");
            if (!File.Exists(fileName))
                throw new FileNotFoundException(fileName);
            TaskCompletionSource<object?> tcs = new();
            Player player = new();
            player.PlaybackFinished += (_, __) => tcs.SetResult(null);
            await player.Play(fileName).ConfigureAwait(false);
            await tcs.Task.ConfigureAwait(false);
        }

        public async Task PlayWindowsMediaFile(string fileName)
        {
            string windir = Environment.GetEnvironmentVariable("windir") ?? throw new InvalidOperationException("Windows directory not found.");
            string path = $"{windir}\\Media\\{fileName}";
            await PlayAudioFile(path).ConfigureAwait(false);
        }

        public async Task Speak(string text, string fileName = "")
        {
            Logger.LogInformation($"Speaking: {text}.");
            if (string.IsNullOrWhiteSpace(text))
                throw new InvalidOperationException("No text to speak.");

            SynthesisInput input = new() { Text = text };
            SynthesizeSpeechRequest request = new()
            {
                Input = input,
                Voice = Voice,
                AudioConfig = new() { AudioEncoding = AudioEncoding.Mp3 }
            };

            // limits entry to single thread
            await TextToSpeechSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                // initiate the request to Google
                Task<SynthesizeSpeechResponse> responseTask = Client.Value.SynthesizeSpeechAsync(request);

                if (fileName != "")
                    await PlayWindowsMediaFile(fileName).ConfigureAwait(false);

                SynthesizeSpeechResponse response = await responseTask.ConfigureAwait(false);

                const string SpeechAudioFileName = "temp.mp3";
                using FileStream fs = File.Create(SpeechAudioFileName);
                {
                    response.AudioContent.WriteTo(fs);
                    fs.Close(); // required since dispose does not close!
                }

                await PlayAudioFile(SpeechAudioFileName).ConfigureAwait(false);
            }
            finally
            {
                TextToSpeechSemaphore.Release();
            }
        }

        public async Task AnnounceTime(ZonedDateTime zdt, string text = "")
        {
            string city = zdt.Zone.Id;
            if (city.Contains("/"))
                city = city.Split("/")[1];
            await AnnounceTime(zdt.LocalDateTime, city, text).ConfigureAwait(false);
        }

        public async Task AnnounceTime(LocalDateTime dt, string locationName = "", string text = "")
        {
            StringBuilder sb = new($"It is now {dt.Date.ToString("MMMM d", null)} at {dt.ToString("H: mm", null)}");
            if (!string.IsNullOrWhiteSpace(locationName))
                sb.Append($"in {locationName}");
            sb.Append(". ");
            if (!string.IsNullOrWhiteSpace(text))
                sb.Append(text + ".");
            await Speak(sb.ToString(), "Alarm01.wav").ConfigureAwait(false);
        }
    }
}
