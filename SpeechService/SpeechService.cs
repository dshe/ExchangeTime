using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using NodaTime;
using Google.Cloud.TextToSpeech.V1;
using NetCoreAudio;

namespace SpeechService
{
    public class Speech
    {
        private readonly VoiceSelectionParams Voice = new VoiceSelectionParams();
        private readonly AudioConfig Config = new AudioConfig();
        private readonly TextToSpeechClient Client = TextToSpeechClient.Create();
        private readonly SemaphoreSlim TextToSpeechSemaphore = new SemaphoreSlim(1);

        public Speech()
        {
            Voice.LanguageCode = "en-US";
            Voice.SsmlGender = SsmlVoiceGender.Neutral;
            Config.AudioEncoding = AudioEncoding.Mp3;
        }

        private void CheckGoogle()
        {
            /*
            Google Cloud Text-to-Speech API. The SDK must be installed.
            https://cloud.google.com/text-to-speech/
            Google services require registration! Monthly free tier: < 4 million characters.
            Use "Service Account Key". IAM and Admin => service accounts for project
            Download a "Service Account JSON file" and then set the environment variable to refer to it.
            */
            var variable = "GOOGLE_APPLICATION_CREDENTIALS";
            var path = Environment.GetEnvironmentVariable(variable);
            if (path == null)
                throw new InvalidOperationException($"{variable} environment variable is not set.");
            if (!File.Exists(path))
                throw new FileNotFoundException($"{path} set in environment variable: {variable}.");
        }

        public async Task PlayAudioFile(string fileName)
        {
            try
            {
                var player = new Player();
                var tcs = new TaskCompletionSource<object?>();
                player.PlaybackFinished += (_, __) => tcs.SetResult(null);
                await player.Play(fileName).ConfigureAwait(false);
                await tcs.Task.ConfigureAwait(false);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task PlayWindowsMediaFile(string fileName)
        {
            var windir = Environment.GetEnvironmentVariable("windir");
            var path = $"{windir}\\Media\\{fileName}";
            if (!File.Exists(path))
                throw new FileNotFoundException(path);
            await PlayAudioFile(path).ConfigureAwait(false);
        }

        public async Task Speak(string text, string fileName = "")
        {
            CheckGoogle();

            var input = new SynthesisInput() { Text = text };
            var request = new SynthesizeSpeechRequest()
            {
                Input = input,
                Voice = Voice,
                AudioConfig = Config
            };

            request.AudioConfig.AudioEncoding = AudioEncoding.Mp3;

            const string SpeechAudioFileName = "temp.mp3";

            // limits entry to single thread
            await TextToSpeechSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                // initiate the request to Google
                var responseTask = Client.SynthesizeSpeechAsync(request);

                if (!string.IsNullOrWhiteSpace(fileName))
                    await PlayWindowsMediaFile(fileName).ConfigureAwait(false);

                var response = await responseTask.ConfigureAwait(false);

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

        public async Task AnnounceTime(ZonedDateTime zdt)
        {
            var tz = zdt.Zone;
            var city = tz.Id.Split("/")[1];
            await AnnounceTime(zdt.LocalDateTime, city).ConfigureAwait(false);
        }

        public async Task AnnounceTime(LocalDateTime dt, string locationName = "", string text = "")
        {
            var sb = new StringBuilder();
            sb.Append($"It is now {dt.Date.ToString("MMMM d", null)} at {dt.ToString("H: mm", null)}");
            if (!string.IsNullOrWhiteSpace(locationName))
                sb.Append($"in {locationName}");
            sb.Append(". ");
            if (!string.IsNullOrWhiteSpace(text))
                sb.Append(text + ".");

            await Speak(sb.ToString(), "Alarm01.wav").ConfigureAwait(false);
        }
    }
}
