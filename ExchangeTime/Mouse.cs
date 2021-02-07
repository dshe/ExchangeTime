using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ExchangeTime
{
    public sealed partial class MainWindow
    {
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
            if (Properties.Settings.Default.Audio)
                await Speech.AnnounceTime(Clock.GetSystemZonedDateTime());

            new MsgBox(this)
            {
                iconType = MsgBox.IconType.Information,
                Background = Brushes.LightGray,
                Title = "ExchangeTime"
            }.Show("Zoom: mouse wheel\nQuit: double-click left mouse button");
        }

        private void MainWindowMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta != 0 && zoomFormats.Zoom(e.Delta > 0))
                Repaint();
            //Logger.Write($"Delta: {e.Delta}.");
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            bool plus = (e.Key == Key.OemPlus || e.Key == Key.Add);
            bool minus = (e.Key == Key.OemMinus || e.Key == Key.Subtract);
            if (!plus && !minus)
                return;
            if (zoomFormats.Zoom(plus))
                Repaint();
        }

        private void MainWindowMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Middle)
                return;
        }
    }
}
