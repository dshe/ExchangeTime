using System;
using System.Reflection;
using System.Threading;
using System.Windows;

namespace ExchangeTime
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            string name = $"{Assembly.GetExecutingAssembly().GetType().GUID}";

            using Mutex mutex = new(true, name, out bool createdNew);
            {
                if (createdNew)
                    new App().Run();
                else
                    MessageBox.Show("Another instance is running!");
            }
        }
    }
}
