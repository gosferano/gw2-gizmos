using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Gw2Gizmos.Desktop.Converters;

/// <summary>
/// Maps a profession id (e.g. "Guardian") to its accent colour — the profession colours the GW2 community uses
/// (wiki, build sites) — for the stripe on a character card. Unknown professions get a muted grey.
/// </summary>
public sealed class ProfessionBrushConverter : IValueConverter
{
    private static readonly IBrush Unknown = new ImmutableSolidColorBrush(Color.FromRgb(0x9E, 0x9E, 0x9E));

    private static readonly Dictionary<string, IBrush> Brushes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Guardian"] = Brush(0x72, 0xC1, 0xD9),
        ["Revenant"] = Brush(0xD1, 0x6E, 0x5A),
        ["Warrior"] = Brush(0xFF, 0xD1, 0x66),
        ["Engineer"] = Brush(0xD0, 0x9C, 0x59),
        ["Ranger"] = Brush(0x8C, 0xDC, 0x82),
        ["Thief"] = Brush(0xC0, 0x8F, 0x95),
        ["Elementalist"] = Brush(0xF6, 0x8A, 0x87),
        ["Mesmer"] = Brush(0xB6, 0x79, 0xD5),
        ["Necromancer"] = Brush(0x52, 0xA7, 0x6F),
    };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string profession ? Brushes.GetValueOrDefault(profession, Unknown) : Unknown;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static IBrush Brush(byte r, byte g, byte b) => new ImmutableSolidColorBrush(Color.FromRgb(r, g, b));
}
