using System;
using System.Diagnostics;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;

namespace ExchangeTime.Utility
{
    public static partial class Sys
    {
        public static void Init(bool singleInstance = false)
        {
            if (Thread.CurrentThread.Name == null)
                Thread.CurrentThread.Name = "MainThread";
            var a = Assembly.GetCallingAssembly();
            Trace.Assert(a != null);
            if (singleInstance && !GlobalInstance.IsSingle(a))
            {
                var msg = new MsgBox
                {
                    iconType = MsgBox.IconType.Error,
                    Title = "ExchangeTime"
                };
                msg.Show("Another instance is running!");
                Environment.Exit(-1);
            }
            InitExceptionHandlers();
        }

        public static void Exit() =>  GlobalInstance.DisposeMutex(); // automatically disposed when the process ends; probably
    }

    public static class GlobalInstance
    {
        private static Mutex mutex;
        private static bool hasMutexHandle;
        internal static bool IsSingle(Assembly a)
        {
            if (mutex != null)
                return true;

            var processName = Process.GetCurrentProcess().ProcessName;
            var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            var mutexId = $"Global\\{{{processName}:{version}}}";

            mutex = new Mutex(false, mutexId);

            var allowEveryoneRule = new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), MutexRights.FullControl, AccessControlType.Allow);
            var securitySettings = new MutexSecurity();
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
            mutex = null;
        }
    }

}
