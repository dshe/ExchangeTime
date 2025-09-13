using HolidayService;
using Jot;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using ExchangeTime.Utility;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
namespace ExchangeTime;

public sealed partial class App : Application
{
    private IHost? MyHost;
    private ILogger Logger = NullLogger.Instance;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        SetHandlers();

        MyHost = CreateHost();
        MyHost.Start();

        Logger = MyHost.Services.GetRequiredService<ILogger<App>>();
        Logger.LogInformation("{AssemblyName}", Assembly.GetExecutingAssembly().FullName);

        MyHost.Services.GetRequiredService<MainWindow>().Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
        MyHost?.Dispose();
    }

    private void SetHandlers()
    {
        AppDomain.CurrentDomain.FirstChanceException += (sender, args) =>
            Logger.LogDebug(args.Exception, "CurrentDomainFirstChanceException: {Message}", args.Exception.Message);

        // Invoked whenever there is an unhandled exception in the default AppDomain.
        // It is invoked for exceptions on any thread that was created on the AppDomain.
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            if (args.ExceptionObject is Exception exception)
                Logger.LogCritical(exception, "CurrentDomainUnhandledException: {Message}", exception.Message);
            else
                Logger.LogCritical("CurrentDomainUnhandledException.");
        };

        // Involed when a faulted task, which has the exception object set, gets collected by the GC.
        // This is useful to track Exceptions in asnync methods where the caller forgets to await the returning task.
        // Note: StackTrace is always null.
        TaskScheduler.UnobservedTaskException += (sender, args) =>
        {
            args.SetObserved();
            Logger.LogError(args.Exception, "TaskSchedulerUnobservedTaskException: {Message}", args.Exception.Message);
            DisplayException(args.Exception);
        };

        // Involved for any unhandled exception on the Dispatcher.
        // When e.RequestCatch is set to true, the exception is caught by the Dispatcher
        // and the DispatcherUnhandledException event will be invoked.
        Dispatcher.UnhandledExceptionFilter += (sender, args) => args.RequestCatch = true;

        // Invoked whenever there is an unhandled exception on a delegate
        // that was posted to be executed on the UI-thread (Dispatcher) of a WPF application.
        Dispatcher.UnhandledException += (sender, args) =>
        {
            args.Handled = true;
            Logger.LogCritical(args.Exception, "DispatcherUnhandledException: {Message}", args.Exception.Message);
            DisplayException(args.Exception);
        };

        static void DisplayException(Exception e) =>
            Current.Dispatcher.BeginInvoke(new ExceptionWindow(e).Show);
    }
    private static IHost CreateHost()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.Configuration.SetBasePath(Directory.GetCurrentDirectory());
        builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
        builder.Services
            .AddSingleton<IClock>(SystemClock.Instance)
            .AddSingleton<AudioService>()
            .AddSingleton<Holidays>()
            .AddSingleton<MainWindow>()
            .AddSingleton(new Tracker())
            //.AddOptions<AppSettings>().BindConfiguration("");
            //.Configure<AppSettings>(builder.Configuration.GetSection(""));
            .Configure<AppSettings>(builder.Configuration);
        return builder.Build();
    }
}
