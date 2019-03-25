using NodaTime;
using System;
using System.Speech.Synthesis;
//using Microsoft.Speech.Synthesis;
//using Windows.Media.SpeechSynthesis;

#nullable enable

namespace ExchangeTime.Utility
{
    public sealed class Speech : IDisposable
    {
        private readonly SpeechSynthesizer synth = new SpeechSynthesizer();

        public Speech()
        {
            synth.SetOutputToDefaultAudioDevice(); // ? new
            synth.Volume = 80;
            //_synth.Rate = 0;
            //var voices = synth.GetInstalledVoices(); // only 2 voices!
            //var voice = voices[0];
            //synth.SelectVoice(voice.VoiceInfo.Name); // does not work
        }

        public void AnnounceTime(ZonedDateTime dt, string locationName = "", string text = "")
        {
            var promptBuilder1 = new PromptBuilder();

            //SystemSounds.Hand.Play(); // don't use SystemSounds because it will only play if defaults sounds are on!
            var audioFile = Environment.GetFolderPath(Environment.SpecialFolder.Windows) + "/Media/ringout.wav";
            promptBuilder1.AppendAudio(audioFile);
            synth.Speak(promptBuilder1);

            var promptBuilder = new PromptBuilder(); // use 2 prompts because we cannot cancel a prompt which starts with an audio file
            //promptBuilder.StartVoice(synth.GetInstalledVoices()[0].VoiceInfo.Name);

            promptBuilder.AppendBreak(PromptBreak.Small);
            promptBuilder.StartSentence();
            //promptBuilder.AppendTextWithHint("You-key, dai job.", SayAs.Text);
            promptBuilder.AppendText("It is now");
            //promptBuilder.StartVoice();
            //promptBuilder.StartStyle();
            //promptBuilder.AppendBreak(PromptBreak.Small);
            //promptBuilder.AppendBreak(new TimeSpan(0, 0, 0, 500));
            //promptBuilder.AppendTextWithHint(dt.Date.ToString("MMM"), SayAs.Month);
            //promptBuilder.AppendTextWithHint(dt.Date.ToString("MMMM d, yyyy"), SayAs.MonthDayYear);
            //promptBuilder.AppendText(now.Date.ToString("MMMM d"));
            promptBuilder.AppendTextWithHint(dt.Date.ToString("MMMM d", null), SayAs.MonthDay);
            promptBuilder.AppendBreak(PromptBreak.Small);
            promptBuilder.AppendText("at");
            promptBuilder.AppendTextWithHint(dt.ToString("hh:mm tt", null), SayAs.Time);
            if (locationName != "")
            {
                promptBuilder.AppendBreak(PromptBreak.Small);
                promptBuilder.AppendText(locationName);
            }
            promptBuilder.EndSentence();
            if (text != "")
            {
                promptBuilder.AppendBreak(PromptBreak.Medium);
                promptBuilder.StartSentence();
                promptBuilder.AppendText(text);
                promptBuilder.EndSentence();
            }
            //promptBuilder.EndVoice();
            synth.SpeakAsync(promptBuilder);
        }

        public void Dispose() => synth.Dispose();
    }
}
