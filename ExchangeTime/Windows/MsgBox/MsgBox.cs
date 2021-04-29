using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ExchangeTime
{
    public class MsgBox
    {
        private readonly Window window = new();
        private Image? image;
        public MsgBox(Window? owner = null)
        {
            if (owner is not null)
            {
                window.Owner = owner;
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            window.ResizeMode = ResizeMode.NoResize;
            window.SizeToContent = SizeToContent.WidthAndHeight;
        }
        public string Title { init { window.Title = value; } }
        public double FontSize { get; init; }
        public SolidColorBrush ForeGround { init { window.Foreground = value; } }
        public SolidColorBrush Background { init { window.Background = value; } }
        public string Buttons { get; init; } = "";
        public enum IconType { Information, Question, Warning, Error };
        public IconType MsgBoxIconType
        {
            init
            {
                string file = $"{Enum.GetName(typeof(IconType), value)}48.png";
                Uri uri = new($"pack://application:,,,/Windows/MessageBox/{file}");
                BitmapImage bmi = new (uri);
                image = new Image
                {
                    Source = bmi,
                    Stretch = Stretch.None,
                    Margin = new Thickness(10)
                };
            }
        }
        public string? Show(string message = "")
        {
            string? result = null;
            Grid grid = new();
            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions.Add(new RowDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            //grid.ShowGridLines = true;

            if (image is not null)
            {
                grid.Children.Add(image);
                Grid.SetRow(image, 0);
                Grid.SetColumn(image, 0);
            }
            if (message != "")
            {
                TextBlock tb = new()
                {
                    Text = message,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(10),
                };
                if (FontSize > 0)
                    tb.FontSize = FontSize;
                grid.Children.Add(tb);
                Grid.SetRow(tb, 0);
                Grid.SetColumn(tb, 1);
            }
            if (Buttons != "")
            {
                TextBlock tb = new()
                {
                    Background = window.Background,
                    Padding = new Thickness(5)
                };
                // note
                grid.Children.Add(tb);
                Grid.SetRow(tb, 1);
                Grid.SetColumn(tb, 0);

                StackPanel panel = new()
                {
                    Background = tb.Background,
                    Orientation = Orientation.Horizontal
                };
                grid.Children.Add(panel);
                Grid.SetRow(panel, 1);
                Grid.SetColumn(panel, 1);

                string[] buttonText = Buttons.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < buttonText.Length; i++)
                {
                    Button button = new()
                    {
                        Padding = new Thickness(3),
                        Margin = new Thickness(6),
                        MinWidth = 70,
                        Content = buttonText[i],
                        IsDefault = i == 0 // the first button is the default
                    };
                    button.Click += (sender, e) => { result = (((sender as Button)?.Content) as string); window.Close(); };
                    panel.Children.Add(button);
                }
                window.WindowStyle = (string.IsNullOrEmpty(window.Title)) ? WindowStyle.ToolWindow : WindowStyle.SingleBorderWindow;
            }
            else
            {
                window.WindowStyle = (string.IsNullOrEmpty(window.Title)) ? WindowStyle.None : WindowStyle.ToolWindow;
                window.MouseDown += (sender, e) => window.Close();
            }
            window.Content = grid;
            window.ShowDialog();
            return result;
        }
    }
}
