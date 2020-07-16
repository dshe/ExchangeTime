using Google.Cloud.TextToSpeech.V1;
using NetCoreAudio;
using System.Threading.Tasks;


namespace SpeechService
{
    public class SpeechTest
    {
        static readonly Speech MySpeech = new Speech();

        public static async Task Main()
        {
            await MySpeech.PlayWindowsMediaFile("Alarm03.wav");

            //await MySpeech.Speak("one");
            //await MySpeech.Speak("two");
            //await MySpeech.Speak("three");

            //await MySpeech.PlayWindowsMediaFile("Alarm03.wav");

            var instant = NodaTime.SystemClock.Instance.GetCurrentInstant();
            var zone = NodaTime.DateTimeZoneProviders.Tzdb.GetSystemDefault();
            var now = instant.InZone(zone).LocalDateTime;

            //await MySpeech.AnnounceTime(now, "Kuala Lumpur", "The KL stock exchange will open in 10 minutes.");
        }
    }
}
