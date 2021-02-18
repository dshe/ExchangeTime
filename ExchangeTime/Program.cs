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
            Mutex mutex = new Mutex(true, name, out bool createdNew);
            try
            {
                if (createdNew)
                    new App().Run();
                else
                    MessageBox.Show("Another instance is running!");
            }
            finally
            {
                mutex.Dispose();
            }
        }
    }
}
