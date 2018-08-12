using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ExchangeTime.Utility
{
    public static class WpfEx
    {
        public static Size GetTextSize(this TextBlock tb)
        {
            var typeface = new Typeface(tb.FontFamily, tb.FontStyle, tb.FontWeight, tb.FontStretch);
            var m_dpiInfo = VisualTreeHelper.GetDpi(tb);
            var pixelsPerDip = m_dpiInfo.PixelsPerDip;
            var formattedText = new FormattedText(tb.Text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, tb.FontSize, Brushes.Black, null, TextFormattingMode.Display, pixelsPerDip);
            return new Size(formattedText.Width, formattedText.Height);
        }

        public static void FitText(this TextBlock tb)
        {
            //Logger.LogInfo(tb.Text +" " + GetTextWidth(tb) + " " + tb.Width);
            var strings = tb.Text.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries); // "Saturday;Sat" -> "Sat"
            foreach (var s in strings)
            {
                tb.Text = s;
                if (tb.GetTextSize().Width < tb.Width)
                    return;
            }
            if (char.IsLetter(strings[0][0])) // first letter of first string
            {
                //Logger.LogInfo("is letter" + strings[0]);
                tb.Text = strings[0].Substring(0, 1); // try first letter
                if (tb.GetTextSize().Width < tb.Width)
                    return;
            }
            tb.Text = null;
        }
    }

}
