using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Gw2Gizmos.Desktop.Converters;

/// <summary>Turns a 0xAARRGGBB colour (as the UI-agnostic core defines its chart colours) into a brush.</summary>
public sealed class ArgbBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is uint argb ? new ImmutableSolidColorBrush(Color.FromUInt32(argb)) : null;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
