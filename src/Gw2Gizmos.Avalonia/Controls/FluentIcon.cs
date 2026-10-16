using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using FluentAvalonia.UI.Controls;

namespace Gw2Gizmos.Avalonia.Controls;

/// <summary>
/// A Fluent System Icons glyph (WPF-UI's <c>SymbolIcon</c>): set <see cref="Symbol"/>, size with FontSize, colour with
/// Foreground.
/// </summary>
public sealed class FluentIcon : TextBlock
{
    public static readonly StyledProperty<FluentSymbol> SymbolProperty =
        AvaloniaProperty.Register<FluentIcon, FluentSymbol>(nameof(Symbol));

    public FluentIcon()
    {
        FontFamily = FluentIconFont.Family;
        VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center;
    }

    public FluentSymbol Symbol
    {
        get => GetValue(SymbolProperty);
        set => SetValue(SymbolProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SymbolProperty)
        {
            Text = FluentIconFont.Glyph(Symbol);
        }
    }
}

/// <summary>
/// A FluentAvalonia icon source for a Fluent System Icons glyph, for controls that take an <c>IconSource</c> (nav
/// items, settings cards): <c>IconSource="{c:FluentIconSource Wallet24}"</c>.
/// </summary>
public sealed class FluentIconSource : MarkupExtension
{
    public FluentIconSource()
    {
    }

    public FluentIconSource(FluentSymbol symbol)
    {
        Symbol = symbol;
    }

    public FluentSymbol Symbol { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider) =>
        new FAFontIconSource { Glyph = FluentIconFont.Glyph(Symbol), FontFamily = FluentIconFont.Family };
}

/// <summary>The bundled Fluent System Icons font.</summary>
internal static class FluentIconFont
{
    public static readonly FontFamily Family =
        new("avares://Gw2Gizmos.Avalonia/Assets/Fonts#FluentSystemIcons-Regular");

    public static string Glyph(FluentSymbol symbol) => char.ConvertFromUtf32((int)symbol);
}
