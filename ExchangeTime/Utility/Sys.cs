using System;
using System.Diagnostics;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;

namespace ExchangeTime
{
    public static partial class Sys
    {
        public static void Init(bool singleInstance = false)
        {
            if (Thread.CurrentThread.Name == null)
                Thread.CurrentThread.Name = "MainThread";
            if (singleInstance && !GlobalInstance.IsSingle())
            {
                new MsgBox
                {
                    iconType = MsgBox.IconType.Error,
                    Title = "ExchangeTime"
                }.Show("Another instance is running!");
                Environment.Exit(-1);
            }
            InitExceptionHandlers();
        }

        public static void Exit() =>  GlobalInstance.DisposeMutex(); // automatically disposed when the process ends; probably
    }

    public static class GlobalInstance
    {
        private static Mutex? mutex = null;
        private static bool hasMutexHandle;
        internal static bool IsSingle()
        {
            if (mutex != null)
                return true;

            string processName = Process.GetCurrentProcess().ProcessName;
            Version version = Assembly.GetExecutingAssembly().GetName().Version ?? throw new Exception("Version is null");
            string mutexId = $"Global\\{{{processName}:{version}}}";

            mutex = new Mutex(false, mutexId);

            MutexAccessRule allowEveryoneRule = new(new SecurityIdentifier(WellKnownSidType.WorldSid, null), MutexRights.FullControl, AccessControlType.Allow);
            MutexSecurity securitySettings = new();
            securitySettings.AddAccessRule(allowEveryoneRule);
            mutex.SetAccessControl(securitySettings);

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
