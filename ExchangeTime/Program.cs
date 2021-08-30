using System;
using System.Threading;
using System.Windows;

namespace ExchangeTime
{
    class Program
    {
        private static readonly string Name = "sYMhtkCo1ECwkg8AimtkMg";

        [STAThread]
        static int Main()
        {
            using Mutex mutex = new(true, Name, out bool createdNew);
            {
                if (createdNew)
                    return new App().Run();

                MessageBox.Show("Another instance is running!");
                return -1;
            }
        }
    }
}
