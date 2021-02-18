using ExchangeTime.Utility;
using HolidayService;
using Jot;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.EventLog;
using NodaTime;
using SpeechService;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace ExchangeTime
{
    public partial class App : Application
    {
        private IHost MyHost = NullHost.Instance;
        private ILogger Logger = NullLogger.Instance; // will be set after generic host initialization

        public App() // the base class constructor is called first
        {
            AppDomain.CurrentDomain.FirstChanceException += (object? sender, FirstChanceExceptionEventArgs e) =>
                Logger.LogWarning($"First Chance Exception\n{e}");

            // Invoked whenever there is an unhandled exception in the default AppDomain.
            // It is invoked for exceptions on any thread that was created on the AppDomain. 
            AppDomain.CurrentDomain.UnhandledException += (object sender, UnhandledExceptionEventArgs args) =>
            {
                Exception e = args.ExceptionObject as Exception ?? new($"AppDomainUnhandledException: Unknown exceptionObject: {args.ExceptionObject}");
                ProcessException($"Current AppDomain Unhandled Exception (IsTerminating = {args.IsTerminating})", e);
            };

            // Invoked whenever there is an unhandled exception on a delegate
            // that was posted to be executed on the UI-thread (Dispatcher) of a WPF application.
            Current.DispatcherUnhandledException += (object sender, DispatcherUnhandledExceptionEventArgs args) =>
            {
                ProcessException("Dispatcher Unhandled Exception", args.Exception);
                args.Handled = true;
            };

            // Invoved for any unhandled exception on the Dispatcher.
            // When e.RequestCatch is set to true, the exception is caught by the Dispatcher
            // and the DispatcherUnhandledException event will be invoked.
            Current.Dispatcher.UnhandledExceptionFilter += (object sender, DispatcherUnhandledExceptionFilterEventArgs args) =>
            {
                args.RequestCatch = true;
            };

            // Involed when a faulted task, which has the exception object set, gets collected by the GC.
            // This is useful to track Exceptions in asnync methods where the caller forgets to await the returning task.
            // Note: StackTrace is always null.
            TaskScheduler.UnobservedTaskException += (object? sender, UnobservedTaskExceptionEventArgs args) =>
            {
                args.SetObserved();
                ProcessException("Unobserved Task Exception", args.Exception);
            };

            void ProcessException(string message, Exception e)
            {
                Logger.LogCritical($"{message}\n{e}\n");
                Current.Dispatcher.BeginInvoke(new Action(() => new ExceptionWindow(e).Show()));
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            MyHost = new HostBuilder()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .ConfigureAppConfiguration((ctx, config) => {
                    config.AddJsonFile("appsettings.json", optional: false);
                })
                .ConfigureLogging((ctx, logging) => {
                    logging.Configure(options => options.ActivityTrackingOptions = ActivityTrackingOptions.SpanId | ActivityTrackingOptions.TraceId | ActivityTrackingOptions.ParentId);
                    logging.SetMinimumLevel(LogLevel.Trace);
                    logging.AddDebug();
                    logging.AddEventLog();
                    logging.AddFilter<EventLogLoggerProvider>(level => level >= LogLevel.Warning);
                    logging.AddEventSourceLogger();
                    logging.AddFile("application.log", config =>
                    {
                        config.Append = true;
                        config.FileSizeLimitBytes = 1_000_000;
                    });
                    logging.AddConfiguration(ctx.Configuration.GetSection("Logging"));
                })
                .ConfigureServices((ctx, services) => {
                    services.Configure<AppSettings>(ctx.Configuration);
                    var clock = new Clock();
                    services.AddSingleton(clock);
                    services.AddSingleton<IClock>(clock);
                    services.AddSingleton<Speech>();
                    services.AddSingleton<Holidays>();
                    services.AddSingleton<MainWindow>();
                    services.AddSingleton(new Tracker());
                })
                .UseDefaultServiceProvider((ctx, options) => {
                    bool isDevelopment = ctx.HostingEnvironment.IsDevelopment();
                    options.ValidateScopes = isDevelopment;
                    options.ValidateOnBuild = isDevelopment;
                })
                .Build();

            Logger = MyHost.Services.GetRequiredService<ILogger<App>>();
            Logger.LogDebug(Assembly.GetExecutingAssembly().FullName);

            base.OnStartup(e); // ?
            MyHost.Services.GetRequiredService<MainWindow>().Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            MyHost.Dispose();
            base.OnExit(e);
        }
    }
}
