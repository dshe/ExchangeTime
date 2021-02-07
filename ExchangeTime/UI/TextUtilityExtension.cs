﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ExchangeTime
{
    public static class TextUtilityExtension
    {
        public static Size GetTextSize(this TextBlock tb)
        {
            Typeface typeface = new(tb.FontFamily, tb.FontStyle, tb.FontWeight, tb.FontStretch);
            DpiScale m_dpiInfo = VisualTreeHelper.GetDpi(tb);
            double pixelsPerDip = m_dpiInfo.PixelsPerDip;
            FormattedText formattedText = new(tb.Text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, tb.FontSize, Brushes.Black, null, TextFormattingMode.Display, pixelsPerDip);
            return new(formattedText.Width, formattedText.Height);
        }

        public static void FitText(this TextBlock tb)
        {
            //Logger.LogInfo(tb.Text +" " + GetTextWidth(tb) + " " + tb.Width);
            string[] strings = tb.Text.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries); // "Saturday;Sat" -> "Sat"
            foreach (string s in strings)
            {
                tb.Text = s;
                if (tb.GetTextSize().Width < tb.Width - 1)
                    return;
            }
            if (char.IsLetter(strings[0][0])) // first letter of first string
            {
                //Logger.LogInfo("is letter" + strings[0]);
                tb.Text = strings[0].Substring(0, 1); // try first letter
                if (tb.GetTextSize().Width < tb.Width - 1)
                    return;
            }
            tb.Text = null;
        }
    }

}
