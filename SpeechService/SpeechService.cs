using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using NodaTime;
using Google.Cloud.TextToSpeech.V1;
using NetCoreAudio;

/*
Google Cloud Text-to-Speech API. The SDK is installed.
https://cloud.google.com/text-to-speech/
Google services require registration!
Monthly free tier = < 4 million characters.
Use "Service Account Key".
IAM and Admin => service accounts for project
Download a "Service Account JSON file" and then set the 
GOOGLE_APPLICATION_CREDENTIALS environment variable to refer to it.
*/

namespace SpeechService
{

    public partial class Speech
    {
        private const string SpeechAudioFileName = "temp.mp3";
        private readonly VoiceSelectionParams Voice = new VoiceSelectionParams();
        private readonly AudioConfig Config = new AudioConfig();
        private readonly TextToSpeechClient Client = TextToSpeechClient.Create();
        private readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1);

        public Speech()
        {
            Voice.LanguageCode = "en-US";
            Voice.SsmlGender = SsmlVoiceGender.Neutral;
            Config.AudioEncoding = AudioEncoding.Mp3;
        }

        public async Task PlayAudioFile(string fileName)
        {
            var tcs = new TaskCompletionSource<object?>();
            var player = new Player();
            player.PlaybackFinished += (_, __) => tcs.SetResult(null);
            await player.Play(fileName).ConfigureAwait(false);
            await tcs.Task.ConfigureAwait(false);
        }

        public async Task PlayWindowsMediaFile(string fileName)
        {
            var path = $"/{Environment.SpecialFolder.Windows}/Media/{fileName}";
            await PlayAudioFile(path).ConfigureAwait(false);
        }

        public async Task Speak(string text, string fileName = "")
        {
            var input = new SynthesisInput() { Text = text };
            var request = new SynthesizeSpeechRequest()
            {
                Input = input,
                Voice = Voice,
                AudioConfig = Config
            };

            // make the request to Google
            var response = Client.SynthesizeSpeech(request);

            // limits entry to single thread
            await Semaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                if (!string.IsNullOrWhiteSpace(fileName))
                    await PlayWindowsMediaFile(fileName).ConfigureAwait(false);

                using (var output = File.Create(SpeechAudioFileName))
                {
                    response.AudioContent.WriteTo(output);
                }

                await PlayAudioFile(SpeechAudioFileName).ConfigureAwait(false);
            }
            finally
            {
                Semaphore.Release();
            }
        }

        public async Task AnnounceTime(LocalDateTime dt, string locationName = "", string text = "")
        {
            var sb = new StringBuilder();
            sb.Append($"It is now {dt.Date.ToString("MMMM d", null)} at {dt.ToString("h: mm", null)}");
            if (!string.IsNullOrWhiteSpace(locationName))
                sb.Append($"in {locationName}");
            sb.Append(". ");
            if (!string.IsNullOrWhiteSpace(text))
                sb.Append(text + ".");

            await Speak(sb.ToString(), "Alarm01.wav").ConfigureAwait(false);
        }
    }
}
