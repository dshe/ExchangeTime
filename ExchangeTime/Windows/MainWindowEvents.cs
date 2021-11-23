using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ExchangeTime;

public partial class MainWindow
{
    private int AudioLocker;
    private void MainWindowMouseDoubleClick(object sender, MouseButtonEventArgs e) => Close();
    private void MainWindowMouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();
    private void MainWindowMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        const int snap = 50;
        if (Top < snap)
            Top = 0;
        else if (Top > SystemParameters.PrimaryScreenHeight - Height - snap)
            Top = SystemParameters.PrimaryScreenHeight - Height - 1;
        if (Left < snap)
            Left = 0;
        else if (Left > SystemParameters.PrimaryScreenWidth - Width - snap)
            Left = SystemParameters.PrimaryScreenWidth - Width;
    }

    private async void MainWindowMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (Interlocked.CompareExchange(ref AudioLocker, 1, 0) == 0)
        {
            await ShowMessage().ConfigureAwait(false);
            AudioLocker = 0;
        }

        async Task ShowMessage()
        {
            Task task = Task.CompletedTask;
            if (Settings.Value.AudioEnable)
                task = Speech.AnnounceTime(Clock.GetCurrentInstant().InZone(TimeZone));
            string version = Assembly.GetEntryAssembly()?.GetName()?.Version?.ToString() ?? "?";
            new MsgBox(this)
            {
                MsgBoxIconType = MsgBox.IconType.Information,
                Background = Brushes.LightGray,
                Title = "ExchangeTime " + version
            }.Show("Zoom: mouse wheel\nQuit: double-click left mouse button");

            await task.ConfigureAwait(false);
        }
    }

    // Zoom using mouse wheel
    private void MainWindowMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Delta != 0 && zoomFormats.Zoom(e.Delta > 0))
            Repaint();
    }

    // Zoom using +/- keys
    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        bool plus = (e.Key == Key.OemPlus || e.Key == Key.Add);
        bool minus = (e.Key == Key.OemMinus || e.Key == Key.Subtract);
        if ((plus || minus) && zoomFormats.Zoom(plus))
            Repaint();
    }

    private void MainWindowMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Middle)
            return;
    }
}
