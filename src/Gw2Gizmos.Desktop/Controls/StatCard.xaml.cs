using System.Windows;
using System.Windows.Controls;

namespace Gw2Gizmos.Desktop.Controls;

/// <summary>
/// A unified dashboard stat card: a title, a large value, and an optional caption sub-line. One control so
/// every count card on the dashboard shares the same look instead of divergent inline markup.
/// </summary>
public partial class StatCard : UserControl
{
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title),
        typeof(string),
        typeof(StatCard),
        new PropertyMetadata("")
    );

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(string),
        typeof(StatCard),
        new PropertyMetadata("")
    );

    public static readonly DependencyProperty CaptionProperty = DependencyProperty.Register(
        nameof(Caption),
        typeof(string),
        typeof(StatCard),
        new PropertyMetadata("")
    );

    // When set, the value is shown as coins (CoinDisplay) instead of the Value string.
    public static readonly DependencyProperty CoinProperty = DependencyProperty.Register(
        nameof(Coin),
        typeof(object),
        typeof(StatCard),
        new PropertyMetadata(null)
    );

    public static readonly DependencyProperty CoinSignedProperty = DependencyProperty.Register(
        nameof(CoinSigned),
        typeof(bool),
        typeof(StatCard),
        new PropertyMetadata(false)
    );

    public StatCard()
    {
        InitializeComponent();
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string Caption
    {
        get => (string)GetValue(CaptionProperty);
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
        get => (bool)GetValue(CoinSignedProperty);
        set => SetValue(CoinSignedProperty, value);
    }
}
