using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Gw2Gizmos.Desktop.Converters;

/// <summary>
/// Maps a bool to <see cref="Visibility"/>: true → Visible, false → Collapsed. Set <see cref="Invert"/>
/// to flip it (true → Collapsed), for showing a placeholder when a flag is false.
/// </summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool flag = value is true;
        if (Invert)
        {
            flag = !flag;
        }

        return flag ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
