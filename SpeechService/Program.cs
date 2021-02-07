using NodaTime;
using System.Threading.Tasks;

namespace SpeechService
{
    public class SpeechTest
    {
        static readonly Speech MySpeech = new();

        public static async Task Main()
        {
            await Speech.PlayWindowsMediaFile("Alarm03.wav");

            //await MySpeech.Speak("one");
            //await MySpeech.Speak("two");
            //await MySpeech.Speak("three");

            //await MySpeech.PlayWindowsMediaFile("Alarm03.wav");

            Instant instant = SystemClock.Instance.GetCurrentInstant();
            DateTimeZone zone = DateTimeZoneProviders.Tzdb.GetSystemDefault();
            LocalDateTime now = instant.InZone(zone).LocalDateTime;

            await MySpeech.AnnounceTime(now, "Kuala Lumpur", "The KL stock exchange will open in 10 minutes.");
        }
    }
}
