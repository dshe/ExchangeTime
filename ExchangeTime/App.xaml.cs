using ExchangeTime.Utility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace ExchangeTime
{
    public partial class App : Application
    {
        public static readonly ILoggerFactory MyLoggerFactory;
        private readonly ILogger Logger;

        static App()
        {
            MyLoggerFactory = LoggerFactory.Create(builder =>
               builder.AddFile("application.log", append: true));
        }

        public App()
        {
            Logger = MyLoggerFactory.CreateLogger("App");
            Logger.LogInformation(Assembly.GetExecutingAssembly().FullName);

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            Dispatcher.UnhandledException += Dispatcher_UnhandledException;
        }

        // AppDomain.CurrentDomain.FirstChanceException
        // Starting with the .NET Framework 4, this event is not raised for exceptions that corrupt the state of the process, such as stack overflows or access violations, unless the event handler is security-critical and has the HandleProcessCorruptedStateExceptionsAttribute attribute.
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            Output("CurrentDomain_UnhandledException");
            if (args.ExceptionObject is Exception exception)
                Output(exception);
            //if (e.IsTerminating)
            //    return;
            //if (Application.Current != null)
            //    Application.Current.Shutdown();
            //else
            //    Environment.Exit(0);
        }

        // Occurs when finalizer is run during a Garbage Collection. Non-deterministic.
        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
        {
            Output("TaskScheduler_UnobservedTaskException");
            Output(args.Exception);
            //if (Application.Current != null)
            //    Application.Current.Shutdown();
            //else
            //    Environment.Exit(0);
        }

        // WPF Application
        private void Dispatcher_UnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs args)
        {
            Output("Dispatcher_UnhandledException");
            Output(args.Exception);
            //MessageBox.Show(e.Exception.Message, "ExchangeTime: Error", MessageBoxButton.OK, MessageBoxImage.Error);
            //if (!e.Dispatcher.HasShutdownStarted)
            //    Application.Current.Shutdown();
        }

        private void Output(Exception e)
        {
            AggregateException? ae = e as AggregateException;
            if (ae != null) // from task
            {
                ae = ae.Flatten();
                foreach (Exception exception in ae.InnerExceptions)
                    Output(exception);
            }
            else
            {
                if (e.InnerException != null)
                    Output(e.InnerException);
                foreach (DictionaryEntry? de in e.Data)
                {
                    if (de == null)
                        throw new Exception("de is null.");
                    Output($"{de.Value.Key}: {de.Value}");
                }
                Output(e);
            }
        }

        private void Output(string message)
        {
            Logger.Log(LogLevel.Critical, message);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            SingleInstance.Check();
			base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
		{
            SingleInstance.DisposeMutex();
			base.OnExit(e);
		}
    }
}
