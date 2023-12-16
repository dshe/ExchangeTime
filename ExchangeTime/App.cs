using HolidayService;
using Jot;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ExchangeTime.Utility;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace ExchangeTime;

public partial class App : Application
{
    private readonly IHost MyHost;
    private readonly ILogger Logger;

    public App() // base class constructor is called first
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
        MyHost = builder.Build();

        Logger = MyHost.Services.GetRequiredService<ILogger<App>>();
        Logger.LogInformation("{AssemblyName}", Assembly.GetExecutingAssembly().FullName);

        AppDomain.CurrentDomain.FirstChanceException += (object? sender, FirstChanceExceptionEventArgs args) =>
            Logger.LogDebug(args.Exception, "CurrentDomainFirstChanceException: {Message}", args.Exception.Message);

        // Invoked whenever there is an unhandled exception in the default AppDomain.
        // It is invoked for exceptions on any thread that was created on the AppDomain.
        AppDomain.CurrentDomain.UnhandledException += (object sender, UnhandledExceptionEventArgs args) =>
        {
            if (args.ExceptionObject is Exception exception)
                Logger.LogCritical(exception, "CurrentDomainUnhandledException: {Message}", exception.Message);
            else
                Logger.LogCritical("CurrentDomainUnhandledException.");
        };

        // Involed when a faulted task, which has the exception object set, gets collected by the GC.
        // This is useful to track Exceptions in asnync methods where the caller forgets to await the returning task.
        // Note: StackTrace is always null.
        TaskScheduler.UnobservedTaskException += (object? sender, UnobservedTaskExceptionEventArgs args) =>
        {
            args.SetObserved();
            Logger.LogError(args.Exception, "TaskSchedulerUnobservedTaskException: {Message}", args.Exception.Message);
            DisplayException(args.Exception);
        };

        // Involved for any unhandled exception on the Dispatcher.
        // When e.RequestCatch is set to true, the exception is caught by the Dispatcher
        // and the DispatcherUnhandledException event will be invoked.
        Dispatcher.UnhandledExceptionFilter += (object sender, DispatcherUnhandledExceptionFilterEventArgs args) => args.RequestCatch = true;

        // Invoked whenever there is an unhandled exception on a delegate
        // that was posted to be executed on the UI-thread (Dispatcher) of a WPF application.
        Dispatcher.UnhandledException += (object sender, DispatcherUnhandledExceptionEventArgs args) =>
        {
            args.Handled = true;
            Logger.LogCritical(args.Exception, "DispatcherUnhandledException: {Message}", args.Exception.Message);
            DisplayException(args.Exception);
        };

        static void DisplayException(Exception e) =>
            Current.Dispatcher.BeginInvoke(new ExceptionWindow(e).Show);
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        MyHost.Services.GetRequiredService<MainWindow>().Show();
        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        MyHost.Dispose();
        base.OnExit(e);
    }
}
