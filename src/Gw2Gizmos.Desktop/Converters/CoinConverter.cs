using System;
using System.Globalization;
using System.Windows.Data;

namespace Gw2Gizmos.Desktop.Converters;

/// <summary>
/// Formats a copper amount (the unit every GW2 price is stored in) as <c>g/s/c</c>, e.g. 12345 → "1g 23s 45c".
/// Accepts any numeric value (prices are <c>int</c>, costs/profit are <c>decimal</c>). One-way only.
/// </summary>
public sealed class CoinConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is null ? string.Empty : Coin.Format((long)Math.Round(System.Convert.ToDecimal(value, culture)));

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
