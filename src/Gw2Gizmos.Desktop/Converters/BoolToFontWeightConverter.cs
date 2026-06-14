using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Gw2Gizmos.Desktop.Converters;

/// <summary>
/// Maps a bool to a <see cref="FontWeight"/>: true → Bold, false → Normal. Used to bold the cheaper of an
/// item's craft-vs-buy price in the recipe tree (neither, when they're equal). One-way only.
/// </summary>
public sealed class BoolToFontWeightConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? FontWeights.Bold : FontWeights.Normal;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
