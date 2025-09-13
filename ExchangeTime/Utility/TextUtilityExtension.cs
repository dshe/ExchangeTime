using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace ExchangeTime.Utility;

internal static class TextUtilityExtension
{
    internal static readonly char[] separator = [';'];
    internal static Size GetTextSize(this TextBlock tb)
    {
        Typeface typeface = new(tb.FontFamily, tb.FontStyle, tb.FontWeight, tb.FontStretch);
        double pixelsPerDip = VisualTreeHelper.GetDpi(tb).PixelsPerDip; // for text scaling

        FormattedText formattedText = new(
            tb.Text,
            CultureInfo.CurrentUICulture, 
            FlowDirection.LeftToRight,
            typeface,
            tb.FontSize, 
            Brushes.Black,
            new NumberSubstitution(),
            TextFormattingMode.Display,
            pixelsPerDip);

        return new(formattedText.Width * 1.3, formattedText.Height);
    }

    internal static void FitText(this TextBlock tb)
    {
        tb.TextWrapping = TextWrapping.NoWrap;
        string[] strings = tb.Text.Split(separator, StringSplitOptions.RemoveEmptyEntries); // "Saturday;Sat" -> "Sat"
        foreach (string s in strings)
        {
            tb.Text = s;
            if (tb.GetTextSize().Width < tb.Width - 1)
                return;
        }
        if (char.IsLetter(strings[0][0])) // first letter of first string
        {
            tb.Text = strings[0][..1]; // try first letter
            if (tb.GetTextSize().Width < tb.Width - 1)
                return;
        }
        tb.Text = "";
    }

}
