using System.Globalization;
using Avalonia.Data.Converters;
using Gw2Gizmos.Desktop.Controls;

namespace Gw2Gizmos.Desktop.Converters;

/// <summary>Turns a Fluent icon's name (as the UI-agnostic core refers to icons, e.g. "Home24") into its symbol.</summary>
public sealed class FluentSymbolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string name && Enum.TryParse(name, out FluentSymbol symbol) ? symbol : FluentSymbol.Search24;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
