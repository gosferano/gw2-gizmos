using System;
using System.Globalization;
using System.Windows.Data;

namespace Gw2Gizmos.Desktop.Converters;

/// <summary>
/// Maps an available pixel width to a dashboard column count so a <c>UniformGrid</c> collapses 3 → 2 → 1
/// as the window narrows (UniformGrid has no width breakpoints of its own; binding its Columns here adds
/// them while it keeps every cell the same size).
/// </summary>
public sealed class WidthToColumnsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        double width = value is double d ? d : 0;
        if (width >= 720)
        {
            return 3;
        }

        return width >= 460 ? 2 : 1;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
