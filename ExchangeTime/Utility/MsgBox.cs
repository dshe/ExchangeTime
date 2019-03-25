using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

#nullable enable

namespace ExchangeTime.Utility
{
    public class MsgBox
    {
        public enum IconType { Information, Question, Warning, Error };
        private readonly Window window = new Window();
        private Image? image = null;
        public MsgBox(Window? owner = null)
        {
            if (owner != null)
            {
                window.Owner = owner;
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            window.ResizeMode = ResizeMode.NoResize;
            window.SizeToContent = SizeToContent.WidthAndHeight;
        }
        public string Title { set { window.Title = value; } }
        public double FontSize { get; set; }
        public SolidColorBrush ForeGround { set { window.Foreground = value; } }
        public SolidColorBrush Background { set { window.Background = value; } }
        public string Buttons { get; set; } = "";
        public IconType iconType
        {
            set
            {
                var path = $"//ExchangeTime;Component/Resources/{Enum.GetName(typeof(IconType), value)}48.png";
                image = new Image
                {
                    Source = new BitmapImage(new Uri(path, UriKind.Relative)),
                    Stretch = Stretch.None,
                    Margin = new Thickness(10)
                };
                // must specify dll;//image.Source = new BitmapImage(new Uri("pack://application:,,," + path));
            }
        }
        public string? Show(string message = "")
        {
            string? result = null;
            var grid = new Grid();
            //grid.ShowGridLines = true;
            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions.Add(new RowDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            if (image != null)
            {
                grid.Children.Add(image);
                Grid.SetRow(image, 0);
                Grid.SetColumn(image, 0);
            }
            if (message != "")
            {
                var tb = new TextBlock
                {
                    Text = message,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(5)
                };
                if (FontSize > 0)
                    tb.FontSize = FontSize;
                grid.Children.Add(tb);
                Grid.SetRow(tb, 0);
                Grid.SetColumn(tb, 1);
            }
            if (Buttons != "")
            {
                var tb = new TextBlock
                {
                    Background = window.Background,
                    Padding = new Thickness(5)
                };
                // note
                grid.Children.Add(tb);
                Grid.SetRow(tb, 1);
                Grid.SetColumn(tb, 0);

                var panel = new StackPanel
                {
                    Background = tb.Background,
                    Orientation = Orientation.Horizontal
                };
                grid.Children.Add(panel);
                Grid.SetRow(panel, 1);
                Grid.SetColumn(panel, 1);

                var buttonText = Buttons.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries); //string[] buttonText = Buttons.Split(';');
                for (var i = 0; i < buttonText.Length; i++)
                {
                    var button = new Button
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
