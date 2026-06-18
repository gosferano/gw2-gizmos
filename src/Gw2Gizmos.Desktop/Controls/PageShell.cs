using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace Gw2Gizmos.Desktop.Controls;

// App (Gw2Gizmos.Desktop) hosts the static navigation entry point used by breadcrumb clicks.
using App = Gw2Gizmos.Desktop.App;

/// <summary>
/// Shared page chrome modelled on the Windows 11 Settings app: a pinned breadcrumb header above a
/// width-capped, horizontally-centered body. A <see cref="Scrollable"/> body (cards, forms) scrolls
/// under the pinned header; a non-scrollable one (a data grid) instead fills the remaining height and
/// scrolls internally. The template lives in App.xaml; every page wraps its content in one of these.
///
/// Every page gets a breadcrumb: pages with flat navigation set just <see cref="Title"/> (one crumb);
/// sub-pages set <see cref="Breadcrumbs"/> for a clickable multi-level trail.
/// </summary>
public class PageShell : ContentControl
{
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title),
        typeof(string),
        typeof(PageShell),
        new PropertyMetadata(null, OnBreadcrumbSourceChanged)
    );

    /// <summary>The page name; becomes a single (non-clickable) crumb when <see cref="Breadcrumbs"/> is unset.</summary>
    public string? Title
    {
        get => (string?)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly DependencyProperty BreadcrumbsProperty = DependencyProperty.Register(
        nameof(Breadcrumbs),
        typeof(IEnumerable<BreadcrumbEntry>),
        typeof(PageShell),
        new PropertyMetadata(null, OnBreadcrumbSourceChanged)
    );

    /// <summary>Optional multi-level trail (e.g. Account › Bank). When unset, one crumb is derived from Title.</summary>
    public IEnumerable<BreadcrumbEntry>? Breadcrumbs
    {
        get => (IEnumerable<BreadcrumbEntry>?)GetValue(BreadcrumbsProperty);
        set => SetValue(BreadcrumbsProperty, value);
    }

    private static readonly DependencyPropertyKey ResolvedBreadcrumbsKey = DependencyProperty.RegisterReadOnly(
        nameof(ResolvedBreadcrumbs),
        typeof(IReadOnlyList<BreadcrumbEntry>),
        typeof(PageShell),
        new PropertyMetadata(Array.Empty<BreadcrumbEntry>())
    );

    public static readonly DependencyProperty ResolvedBreadcrumbsProperty = ResolvedBreadcrumbsKey.DependencyProperty;

    /// <summary>The crumbs the template's breadcrumb bar binds to.</summary>
    public IReadOnlyList<BreadcrumbEntry> ResolvedBreadcrumbs
    {
        get => (IReadOnlyList<BreadcrumbEntry>)GetValue(ResolvedBreadcrumbsProperty);
        private set => SetValue(ResolvedBreadcrumbsKey, value);
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

    public PageShell()
    {
        // Highlight the nav section this page belongs under: its root breadcrumb's Target (sub-pages declare it);
        // top-level pages pass null and App falls back to the rail's own selection. Driven here so any page — and
        // any future hierarchy — keeps the rail in sync with no extra wiring.
        Loaded += (_, _) =>
            App.HighlightNavSection(ResolvedBreadcrumbs.Count > 0 ? ResolvedBreadcrumbs[0].Target : null);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        if (GetTemplateChild("PART_Breadcrumb") is BreadcrumbBar bar)
        {
            bar.ItemClicked -= OnBreadcrumbItemClicked;
            bar.ItemClicked += OnBreadcrumbItemClicked;
        }
    }

    private void OnBreadcrumbItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
    {
        if (args.Item is BreadcrumbEntry { Target: { } target })
        {
            App.NavigateTo(target);
        }
    }

    private static void OnBreadcrumbSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((PageShell)d).UpdateResolvedBreadcrumbs();

    private void UpdateResolvedBreadcrumbs()
    {
        if (Breadcrumbs is not null)
        {
            ResolvedBreadcrumbs = Breadcrumbs as IReadOnlyList<BreadcrumbEntry> ?? new List<BreadcrumbEntry>(Breadcrumbs);
        }
        else if (!string.IsNullOrEmpty(Title))
        {
            ResolvedBreadcrumbs = new[] { new BreadcrumbEntry { Title = Title } };
        }
        else
        {
            ResolvedBreadcrumbs = Array.Empty<BreadcrumbEntry>();
        }
    }
}
