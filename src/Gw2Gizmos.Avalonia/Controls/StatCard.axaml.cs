using Avalonia;
using Avalonia.Controls;

namespace Gw2Gizmos.Avalonia.Controls;

/// <summary>
/// A unified dashboard stat card: a title, a large value, and an optional caption sub-line. One control so every
/// count card on the dashboard shares the same look instead of divergent inline markup.
/// </summary>
public partial class StatCard : UserControl
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<StatCard, string>(nameof(Title), "");

    public static readonly StyledProperty<string> ValueProperty =
        AvaloniaProperty.Register<StatCard, string>(nameof(Value), "");

    public static readonly StyledProperty<string> CaptionProperty =
        AvaloniaProperty.Register<StatCard, string>(nameof(Caption), "");

    // When set, the value is shown as coins (CoinDisplay) instead of the Value string.
    public static readonly StyledProperty<object?> CoinProperty =
        AvaloniaProperty.Register<StatCard, object?>(nameof(Coin));

    public static readonly StyledProperty<bool> CoinSignedProperty =
        AvaloniaProperty.Register<StatCard, bool>(nameof(CoinSigned));

    public StatCard()
    {
        InitializeComponent();
    }

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string Caption
    {
        get => GetValue(CaptionProperty);
        set => SetValue(CaptionProperty, value);
    }

    /// <summary>A copper amount to render as coins in place of <see cref="Value"/>; null shows the string value.</summary>
    public object? Coin
    {
        get => GetValue(CoinProperty);
        set => SetValue(CoinProperty, value);
    }

    /// <summary>Whether the coin value shows a +/− sign (for gains/losses).</summary>
    public bool CoinSigned
    {
        get => GetValue(CoinSignedProperty);
        set => SetValue(CoinSignedProperty, value);
    }
}
