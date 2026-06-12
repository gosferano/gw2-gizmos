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
}
