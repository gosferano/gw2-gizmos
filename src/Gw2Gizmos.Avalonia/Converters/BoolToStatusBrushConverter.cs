using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Gw2Gizmos.Avalonia.Converters;

/// <summary>
/// Maps a bool to a small status-dot brush: true → green ("healthy"), false → a muted grey. Used for the dashboard
/// API-key and Worker indicators. Brushes are immutable so they're shared safely.
/// </summary>
public sealed class BoolToStatusBrushConverter : IValueConverter
{
    private static readonly IBrush On = new ImmutableSolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50));
    private static readonly IBrush Off = new ImmutableSolidColorBrush(Color.FromArgb(0x66, 0x9E, 0x9E, 0x9E));

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? On : Off;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
