using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ExchangeTime.Utility;

internal static class TextUtilityExtension
{
    internal static Size GetTextSize(this TextBlock tb)
    {
        DpiScale m_dpiInfo = VisualTreeHelper.GetDpi(tb);
        double pixelsPerDip = m_dpiInfo.PixelsPerDip;

        Typeface typeface = new(tb.FontFamily, tb.FontStyle, tb.FontWeight, tb.FontStretch);

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

        return new(formattedText.Width * 1.2, formattedText.Height);
    }

    internal static void FitText(this TextBlock tb)
    {
        tb.TextWrapping = TextWrapping.NoWrap;
        string[] strings = tb.Text.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries); // "Saturday;Sat" -> "Sat"
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
