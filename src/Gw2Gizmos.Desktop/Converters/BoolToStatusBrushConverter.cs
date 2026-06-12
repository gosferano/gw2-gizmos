using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Gw2Gizmos.Desktop.Converters;

/// <summary>
/// Maps a bool to a small status-dot brush: true → green ("healthy"), false → a muted grey. Used for the
/// dashboard API-key and Worker indicators. Brushes are frozen so they're shared safely.
/// </summary>
public sealed class BoolToStatusBrushConverter : IValueConverter
{
    private static readonly Brush On = Frozen(Color.FromRgb(0x4C, 0xAF, 0x50));
    private static readonly Brush Off = Frozen(Color.FromArgb(0x66, 0x9E, 0x9E, 0x9E));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? On : Off;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static Brush Frozen(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
