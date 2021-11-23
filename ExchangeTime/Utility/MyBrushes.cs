using System.Windows.Media;

namespace ExchangeTime.Utility;

internal static class MyBrushes
{
    internal static readonly Brush Gray48 = CreateGrayBrush(48);
    internal static readonly Brush Gray96 = CreateGrayBrush(96);
    internal static readonly Brush Gray128 = CreateGrayBrush(128);
    internal static readonly Brush Gray224 = CreateGrayBrush(224);
    private static SolidColorBrush CreateGrayBrush(byte b) => new(Color.FromRgb(b, b, b));

    private static readonly BrushConverter BrushConverter = new();

    internal static SolidColorBrush CreateBrush(string color) =>
        BrushConverter.ConvertFromString(color) as SolidColorBrush ??
            throw new InvalidOperationException("Could not convert to SolidColorBrush: " + color + ".");
}
