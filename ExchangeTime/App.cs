using HolidayService;
using Jot;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;
using NodaTime;
using SpeechService;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
namespace ExchangeTime;
// changed build action for this file from 'ApplicationDefinition' to 'C# compiler'

public partial class App : Application
{
    private readonly IHost MyHost;
    private readonly ILogger Logger;

    public App() // base class constructor is called first
    {
        MyHost = new HostBuilder()
            .ConfigureAppConfiguration((ctx, config) => config
                .SetBasePath(ctx.HostingEnvironment.ContentRootPath)
                .AddJsonFile($"appsettings.json", optional: false))
            .ConfigureLogging((ctx, logging) => logging
                .Configure(options => options.ActivityTrackingOptions = ActivityTrackingOptions.SpanId | ActivityTrackingOptions.TraceId | ActivityTrackingOptions.ParentId)
                .SetMinimumLevel(LogLevel.Information)
                .AddDebug()
                .AddEventLog()
                .AddFilter<EventLogLoggerProvider>(level => level >= LogLevel.Warning)
                .AddEventSourceLogger()
                .AddEventLog() 
                .AddFile("application.log", config => // NReco.Logging dependency
                {
                    config.Append = true;
                    config.FileSizeLimitBytes = 10_000_000;
                })
                .AddConfiguration(ctx.Configuration.GetSection("Logging")))
            .ConfigureServices((ctx, services) => services
                .Configure<AppSettings>(ctx.Configuration)
                .AddSingleton<IClock>(SystemClock.Instance)
                .AddSingleton<Speech>()
                .AddSingleton<Holidays>()
                .AddSingleton<MainWindow>()
                .AddSingleton(new Tracker()))
            .UseDefaultServiceProvider((ctx, options) =>
            {
                options.ValidateScopes = ctx.HostingEnvironment.IsDevelopment();
                options.ValidateOnBuild = ctx.HostingEnvironment.IsDevelopment();
            })
            .Build();

        Logger = MyHost.Services.GetRequiredService<ILogger<App>>();
        Logger.LogInformation(Assembly.GetExecutingAssembly().FullName);

        AppDomain.CurrentDomain.FirstChanceException += (object? sender, FirstChanceExceptionEventArgs args) =>
            Logger.LogInformation(args.Exception, $"CurrentDomainFirstChanceException: {args.Exception.Message}");

        // Invoked whenever there is an unhandled exception in the default AppDomain.
        // It is invoked for exceptions on any thread that was created on the AppDomain.
        AppDomain.CurrentDomain.UnhandledException += (object sender, UnhandledExceptionEventArgs args) =>
        {
            if (args.ExceptionObject is Exception exception)
                Logger.LogCritical(exception, $"CurrentDomainUnhandledException: {exception.Message}");
            else
                Logger.LogCritical("CurrentDomainUnhandledException.");
        };

        // Involed when a faulted task, which has the exception object set, gets collected by the GC.
        // This is useful to track Exceptions in asnync methods where the caller forgets to await the returning task.
        // Note: StackTrace is always null.
        TaskScheduler.UnobservedTaskException += (object? sender, UnobservedTaskExceptionEventArgs args) =>
        {
            args.SetObserved();
            Logger.LogError(args.Exception, $"TaskSchedulerUnobservedTaskException: {args.Exception.Message}");
            DisplayException(args.Exception);
        };

        // Invoved for any unhandled exception on the Dispatcher.
        // When e.RequestCatch is set to true, the exception is caught by the Dispatcher
        // and the DispatcherUnhandledException event will be invoked.
        Dispatcher.UnhandledExceptionFilter += (object sender, DispatcherUnhandledExceptionFilterEventArgs args) =>
        {
            args.RequestCatch = true;
        };

        // Invoked whenever there is an unhandled exception on a delegate
        // that was posted to be executed on the UI-thread (Dispatcher) of a WPF application.
        Dispatcher.UnhandledException += (object sender, DispatcherUnhandledExceptionEventArgs args) =>
        {
            args.Handled = true;
            Logger.LogCritical(args.Exception, $"DispatcherUnhandledException: {args.Exception.Message}");
            DisplayException(args.Exception);
        };

        void DisplayException(Exception e) =>
            Current.Dispatcher.BeginInvoke(new Action(() => new ExceptionWindow(e).Show()));
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await MyHost.StartAsync().ConfigureAwait(false);
        MyHost.Services.GetRequiredService<MainWindow>().Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await MyHost.StopAsync().ConfigureAwait(false);
        MyHost.Dispose();
        base.OnExit(e);
    }
}
