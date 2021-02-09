using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;

namespace ExchangeTime.Utility
{
    public static class SingleInstance
    {
        private static Mutex? mutex = null;
        private static bool hasMutexHandle;

        public static void Check()
        {
            if (Thread.CurrentThread.Name == null)
                Thread.CurrentThread.Name = "MainThread";
            if (!IsSingle())
            {
                var msg = "Another instance is running!";
                var logger = App.MyLoggerFactory.CreateLogger("SingleInstance");
                logger.LogCritical(msg);

                new MsgBox
                {
                    MsgBoxIconType = MsgBox.IconType.Error,
                    Title = "ExchangeTime"
                }.Show(msg);
                Environment.Exit(-1);
            }
        }

        private static bool IsSingle()
        {
            if (mutex != null)
                return true;

            string processName = Process.GetCurrentProcess().ProcessName;
            Version version = Assembly.GetExecutingAssembly().GetName().Version ?? throw new Exception("Version is null");
            string mutexId = $"Global\\{{{processName}:{version}}}";

            mutex = new Mutex(false, mutexId);

            try
            {
                hasMutexHandle = mutex.WaitOne(0, false);
                if (!hasMutexHandle)
                {
                    DisposeMutex();
                    return false; //throw new TimeoutException("Timeout waiting for exclusive access on SingleInstance");
                }
            }
            catch (AbandonedMutexException)
            {
                hasMutexHandle = true;
            }
            return true;
        }

        public static void DisposeMutex()
        {
            if (mutex == null)
                return;
            if (hasMutexHandle)
                mutex.ReleaseMutex();
            mutex.Dispose();
        }
    }
}
