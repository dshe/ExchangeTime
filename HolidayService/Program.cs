using NodaTime;
using System;
using System.Threading.Tasks;

namespace HolidayService
{
    class Program
    {
        static readonly IClock Clock = SystemClock.Instance;

        static async Task Main(string[] args)
        {
            var holidays = new Holidays(Clock);

            await holidays.LoadHolidays("usa", "ny");

            var holiday = holidays.TryGetHoliday("usa", "ny", new LocalDate(2019, 12, 25));
            if (holiday != null)
                Console.WriteLine(holiday.Name);

            await TestEnrico();
        }

        static async Task TestEnrico()
        {
            var holidayService = new Enrico(SystemClock.Instance, 1);

            var clock = SystemClock.Instance;

            var now = clock.GetCurrentInstant().InUtc();
            var from = now.Minus(Duration.FromDays(30)).Date;
            var to = now.Plus(Duration.FromDays(90)).Date;

            var str = await holidayService.GetHolidays("usa", "ny", from, to);
        }

    }
}
