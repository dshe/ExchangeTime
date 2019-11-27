using System;
using System.Collections;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;

namespace ExchangeTime
{
    public static partial class Sys
    {
        private static void InitExceptionHandlers()
        {
            //AppDomain.CurrentDomain.FirstChanceException += ((sender, e) => Logger.LogInfo(new Exception("FirstChanceException in " + AppDomain.CurrentDomain.FriendlyName, e.Exception)));

            //Starting with the .NET Framework 4, this event is not raised for exceptions that corrupt the state of the process, such as stack overflows or access violations, unless the event handler is security-critical and has the HandleProcessCorruptedStateExceptionsAttribute attribute.
            AppDomain.CurrentDomain.UnhandledException += (o, e) => // does not capture all exceptios; else does not capture when debugging
            {
                var ex = (Exception)e.ExceptionObject;
                LogFatalExceptions("AppDomain.UnhandledException", ex);
                if (e.IsTerminating)
                    return;
                if (Application.Current != null)
                    Application.Current.Shutdown();
                else
                    Environment.Exit(0);
            };

            // occurs when finalizer is run during a Garbage Collection. This non-deterministic.
            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                if (e.Exception == null)
                    throw new Exception("Exception is null.");
                LogFatalExceptions("UnobservedTaskException", e.Exception);
                //e.SetObserved(); // ?
                if (Application.Current != null)
                    Application.Current.Shutdown();
                else
                    Environment.Exit(0);

            };

            // WPF Application
            if (Application.Current != null)
            {
                Application.Current.DispatcherUnhandledException += (sender, e) =>
                {
                    Debug.Write(e.Exception, "Dispatcher UnhandledException: Fatal Error: " + e.Exception.Message);
                    e.Handled = true; // ?

                    // new
                    MessageBox.Show(e.Exception.Message, "ExchangeTime: Error", MessageBoxButton.OK, MessageBoxImage.Error);

                    if (!e.Dispatcher.HasShutdownStarted)
                        Application.Current.Shutdown();
                };
            }
        }

        private static void LogFatalExceptions(string msg, Exception e)
        {
            var ae = e as AggregateException;
            if (ae != null) // from task
            {
                ae = ae.Flatten();
                foreach (var exception in ae.InnerExceptions)
                    LogFatalExceptions(msg, exception);
            }
            else
            {
                if (e.InnerException != null)
                    LogFatalExceptions(msg, e.InnerException);
                foreach (DictionaryEntry? de in e.Data)
                {
                    if (de == null)
                        throw new Exception("de is null.");
                    Debug.WriteLine("{0}: {1}", de.Value.Key, de.Value);
                }
                Debug.WriteLine(e, msg);
            }
        }
    }
}
