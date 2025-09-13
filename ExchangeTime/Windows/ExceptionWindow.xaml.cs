using System.Windows;
namespace ExchangeTime;

public sealed partial class ExceptionWindow : Window
{
    public Exception Exception { get; }
    public string ExceptionType { get; }

    internal ExceptionWindow(Exception e)
    {
        Exception = e;
        ExceptionType = e.GetType().FullName ?? "";
        InitializeComponent();
        DataContext = this;
    }

    internal void OnExitAppClick(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
    internal void OnExceptionWindowClosed(object sender, EventArgs e) => Application.Current.Shutdown();
}
