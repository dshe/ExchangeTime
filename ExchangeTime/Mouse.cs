using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ExchangeTime.Utility;
using NodaTime;

namespace ExchangeTime
{
    public sealed partial class MainWindow : IDisposable
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

        private void MainWindowMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
			if (Properties.Settings.Default.Audio)
				speech.AnnounceTime(Clock.SystemTime);

            new MsgBox(this)
            {
                iconType = MsgBox.IconType.Information,
                Background = Brushes.LightGray,
                Title = "ExchangeTime"
            }.Show("Zoom: mouse wheel\nQuit: double-click left mouse button");
        }

        private void MainWindowMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta < 0)
            {
                if (formatIndex != 0)
                    formatIndex--;
            }
            else if (e.Delta > 0)
            {
                if (formatIndex < formats.Count - 1)
                    formatIndex++;
            }
            Repaint();
        }

        private void MainWindowMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Middle)
                return;
        }

        public void Dispose() => speech.Dispose();
    }
}
