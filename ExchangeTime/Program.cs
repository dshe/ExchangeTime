using System.Threading;
using System.Windows;

namespace ExchangeTime;

class Program
{
    private const string Name = "sYMhtkCo1ECwkg8AimtkMg";

    [STAThread]
    static int Main()
    {
        using Mutex mutex = new(true, Name, out bool createdNew);
        {
            if (!createdNew)
            {
                MessageBox.Show("Another instance is running!");
                return -1;
            }

            return new App().Run();
        }
    }
}
