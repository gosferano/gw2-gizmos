using System;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace Gw2Gizmos.Desktop.Converters;

/// <summary>
/// Formats a copper amount (the unit every GW2 price is stored in) as <c>g/s/c</c>, e.g. 12345 → "1g 23s 45c".
/// Accepts any numeric value (prices are <c>int</c>, costs/profit are <c>decimal</c>). One-way only.
/// </summary>
public sealed class CoinConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
        {
            return string.Empty;
        }

        long total = (long)Math.Round(System.Convert.ToDecimal(value, culture));
        bool negative = total < 0;
        total = Math.Abs(total);

        long gold = total / 10000;
        long silver = total % 10000 / 100;
        long copper = total % 100;

        var sb = new StringBuilder();
        if (negative)
        {
            sb.Append('-');
        }

        if (gold > 0)
        {
            sb.Append(gold).Append("g ");
        }

        if (gold > 0 || silver > 0)
        {
            sb.Append(silver).Append("s ");
        }

        sb.Append(copper).Append('c');
        return sb.ToString();
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
