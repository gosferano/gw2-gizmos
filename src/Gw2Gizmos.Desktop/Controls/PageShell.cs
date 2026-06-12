using System.Windows;
using System.Windows.Controls;

namespace Gw2Gizmos.Desktop.Controls;

/// <summary>
/// Shared page chrome modelled on the Windows 11 Settings app: a pinned breadcrumb/title header above a
/// width-capped, horizontally-centered body. A <see cref="Scrollable"/> body (cards, forms) scrolls
/// under the pinned header; a non-scrollable one (a data grid) instead fills the remaining height and
/// scrolls internally. The template lives in App.xaml; every page wraps its content in one of these.
/// </summary>
public class PageShell : ContentControl
{
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title),
        typeof(string),
        typeof(PageShell)
    );

    /// <summary>The pinned header text (the breadcrumb for this flat navigation).</summary>
    public string? Title
    {
        get => (string?)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly DependencyProperty ScrollableProperty = DependencyProperty.Register(
        nameof(Scrollable),
        typeof(bool),
        typeof(PageShell),
        new PropertyMetadata(true)
    );

    /// <summary>True (default): the body scrolls under the pinned header. False: the body fills the
    /// remaining height (grid pages, which scroll their own rows).</summary>
    public bool Scrollable
    {
        get => (bool)GetValue(ScrollableProperty);
        set => SetValue(ScrollableProperty, value);
    }

    public static readonly DependencyProperty MaxContentWidthProperty = DependencyProperty.Register(
        nameof(MaxContentWidth),
        typeof(double),
        typeof(PageShell),
        new PropertyMetadata(1100.0)
    );

    /// <summary>Width the header and body are capped at before centering on wide windows.</summary>
    public double MaxContentWidth
    {
        get => (double)GetValue(MaxContentWidthProperty);
        set => SetValue(MaxContentWidthProperty, value);
    }
}
