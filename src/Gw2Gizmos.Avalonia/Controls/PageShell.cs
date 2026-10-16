using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Controls;
using Gw2Gizmos.Avalonia.Navigation;
using Gw2Gizmos.Desktop.Core.Navigation;

namespace Gw2Gizmos.Avalonia.Controls;

/// <summary>
/// Shared page chrome modelled on the Windows 11 Settings app: a pinned breadcrumb header above a width-capped,
/// horizontally-centered body. A <see cref="Scrollable"/> body (cards, forms) scrolls under the pinned header; a
/// non-scrollable one (a data grid) instead fills the remaining height and scrolls internally. The template lives
/// in Themes/Controls.axaml; every page wraps its content in one of these.
///
/// Every page gets a breadcrumb: pages with flat navigation set just <see cref="Title"/> (one crumb); sub-pages set
/// <see cref="Breadcrumbs"/> for a clickable multi-level trail.
/// </summary>
[PseudoClasses(":fixed")]
public class PageShell : ContentControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<PageShell, string?>(nameof(Title));

    public static readonly StyledProperty<IEnumerable<BreadcrumbEntry>?> BreadcrumbsProperty =
        AvaloniaProperty.Register<PageShell, IEnumerable<BreadcrumbEntry>?>(nameof(Breadcrumbs));

    public static readonly DirectProperty<PageShell, IReadOnlyList<BreadcrumbEntry>> ResolvedBreadcrumbsProperty =
        AvaloniaProperty.RegisterDirect<PageShell, IReadOnlyList<BreadcrumbEntry>>(
            nameof(ResolvedBreadcrumbs), shell => shell.ResolvedBreadcrumbs);

    public static readonly StyledProperty<bool> ScrollableProperty =
        AvaloniaProperty.Register<PageShell, bool>(nameof(Scrollable), true);

    public static readonly StyledProperty<double> MaxContentWidthProperty =
        AvaloniaProperty.Register<PageShell, double>(nameof(MaxContentWidth), 1100.0);

    private IReadOnlyList<BreadcrumbEntry> _resolvedBreadcrumbs = Array.Empty<BreadcrumbEntry>();
    private FABreadcrumbBar? _breadcrumbBar;

    /// <summary>The page name; becomes a single (non-clickable) crumb when <see cref="Breadcrumbs"/> is unset.</summary>
    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>Optional multi-level trail (e.g. Account › Bank). When unset, one crumb is derived from Title.</summary>
    public IEnumerable<BreadcrumbEntry>? Breadcrumbs
    {
        get => GetValue(BreadcrumbsProperty);
        set => SetValue(BreadcrumbsProperty, value);
    }

    /// <summary>The crumbs the template's breadcrumb bar binds to.</summary>
    public IReadOnlyList<BreadcrumbEntry> ResolvedBreadcrumbs
    {
        get => _resolvedBreadcrumbs;
        private set => SetAndRaise(ResolvedBreadcrumbsProperty, ref _resolvedBreadcrumbs, value);
    }

    /// <summary>True (default): the body scrolls under the pinned header. False: the body fills the remaining height
    /// (grid pages, which scroll their own rows).</summary>
    public bool Scrollable
    {
        get => GetValue(ScrollableProperty);
        set => SetValue(ScrollableProperty, value);
    }

    /// <summary>Width the header and body are capped at before centering on wide windows.</summary>
    public double MaxContentWidth
    {
        get => GetValue(MaxContentWidthProperty);
        set => SetValue(MaxContentWidthProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TitleProperty || change.Property == BreadcrumbsProperty)
        {
            UpdateResolvedBreadcrumbs();
        }
        else if (change.Property == ScrollableProperty)
        {
            PseudoClasses.Set(":fixed", !Scrollable);
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_breadcrumbBar is not null)
        {
            _breadcrumbBar.ItemClicked -= OnBreadcrumbItemClicked;
        }

        _breadcrumbBar = e.NameScope.Find<FABreadcrumbBar>("PART_Breadcrumb");
        if (_breadcrumbBar is not null)
        {
            _breadcrumbBar.ItemClicked += OnBreadcrumbItemClicked;
        }
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        // Highlight the nav section this page belongs under: its root breadcrumb's Target (sub-pages declare it);
        // top-level pages pass null and the rail keeps its own selection. Driven here so any page — and any future
        // hierarchy — keeps the rail in sync with no extra wiring.
        AppServices.Navigation?.HighlightSection(ResolvedBreadcrumbs.Count > 0 ? ResolvedBreadcrumbs[0].Target : null);
    }

    private void OnBreadcrumbItemClicked(FABreadcrumbBar sender, FABreadcrumbBarItemClickedEventArgs args)
    {
        if (args.Item is BreadcrumbEntry { Target: { } target })
        {
            AppServices.Navigation?.NavigateTo(target);
        }
    }

    private void UpdateResolvedBreadcrumbs()
    {
        if (Breadcrumbs is not null)
        {
            ResolvedBreadcrumbs = Breadcrumbs as IReadOnlyList<BreadcrumbEntry> ?? Breadcrumbs.ToList();
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
