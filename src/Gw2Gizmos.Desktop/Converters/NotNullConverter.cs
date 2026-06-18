using System;
using System.Globalization;
using System.Windows.Data;

namespace Gw2Gizmos.Desktop.Converters;

/// <summary>
/// True when the bound value is non-null, false when null. Used to toggle between alternate presentations
/// (e.g. a StatCard showing a coin value vs. a plain string) based on whether an optional value is set.
/// </summary>
public sealed class NotNullConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is not null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
