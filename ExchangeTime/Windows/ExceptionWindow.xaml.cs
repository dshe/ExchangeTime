using System;
using System.Windows;

namespace ExchangeTime.Windows
{
    public partial class ExceptionWindow : Window
    {
        public Exception Exception { get; }
        public string ExceptionType { get; }

        public ExceptionWindow(Exception e)
        {
            Exception = e;
            ExceptionType = e.GetType().FullName ?? "";
            InitializeComponent();
            DataContext = this;
        }

        public void OnExitAppClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        public void OnExceptionWindowClosed(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
