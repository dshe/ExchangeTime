using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace ExchangeTime
{
	public partial class App : Application
    {
        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            Dispatcher.UnhandledException += Dispatcher_UnhandledException;
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            Output("CurrentDomain_UnhandledException");
            if (args.ExceptionObject is Exception exception)
                Output(exception.ToString());
        }

        private static void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
        {
            Output("TaskScheduler_UnobservedTaskException");
            if (args.Exception is Exception exception)
                Output(exception.ToString());
        }

        private static void Dispatcher_UnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs args)
        {
            Output("Dispatcher_UnhandledException");
            if (args.Exception is Exception exception)
                Output(exception.ToString());
        }

        public static void Output(string message)
        {
            File.AppendAllText("error.log", $"{DateTime.Now}:\n{message}\n\n");
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            Sys.Init(true);
			base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
		{
			Sys.Exit();
			base.OnExit(e);
		}
    }
}
