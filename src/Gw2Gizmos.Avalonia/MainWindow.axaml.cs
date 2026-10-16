using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using Gw2Gizmos.Avalonia.Navigation;
using Gw2Gizmos.Desktop.Core;
using Gw2Gizmos.Desktop.Core.Navigation;

namespace Gw2Gizmos.Avalonia;

/// <summary>The app shell: a Fluent navigation rail whose items open pages created by the <see cref="PageFactory"/>.</summary>
public partial class MainWindow : Window
{
    private readonly PageFactory _pages = new();
    private AppPage? _current;
    private bool _syncingSelection;

    /// <summary>Parameterless constructor for the XAML previewer.</summary>
    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(AvaloniaNavigationService navigation, OnboardingStore onboarding)
        : this()
    {
        navigation.Attach(this);
        RootNavigation.SelectionChanged += OnSelectionChanged;

        // First launch lands on the one-time welcome/onboarding page; afterwards straight to the dashboard.
        ShowPage(StartPageOverride() ?? (onboarding.IsCompleted ? AppPage.Dashboard : AppPage.Onboarding));
    }

    /// <summary>Debug builds: <c>--start-page=Wallet</c> opens that page directly (handy while porting pages).</summary>
    private static AppPage? StartPageOverride()
    {
#if DEBUG
        const string prefix = "--start-page=";
        string? arg = Environment.GetCommandLineArgs().FirstOrDefault(a => a.StartsWith(prefix, StringComparison.Ordinal));
        if (arg is not null && Enum.TryParse(arg[prefix.Length..], ignoreCase: true, out AppPage page))
        {
            return page;
        }
#endif
        return null;
    }

    /// <summary>Shows <paramref name="page"/> and highlights its rail item (if it has one).</summary>
    public void ShowPage(AppPage page)
    {
        if (_current == page)
        {
            return;
        }

        _current = page;
        RootNavigation.Content = _pages.Create(page);

        // Keep the rail in sync when navigation comes from code. Sub-pages without their own rail item leave the
        // selection to their PageShell, which highlights their section (root breadcrumb) once loaded.
        SelectRailItem(page);
    }

    /// <summary>Selects <paramref name="page"/>'s rail item, if it has one, without navigating again.</summary>
    public void SelectRailItem(AppPage page)
    {
        FANavigationViewItem? item = RootNavigation.MenuItems
            .Concat(RootNavigation.FooterMenuItems)
            .OfType<FANavigationViewItem>()
            .FirstOrDefault(candidate => candidate.Tag is AppPage tag && tag == page);
        if (item is null || ReferenceEquals(RootNavigation.SelectedItem, item))
        {
            return;
        }

        // Selecting raises SelectionChanged, which would navigate to the section itself; suppress that.
        _syncingSelection = true;
        try
        {
            RootNavigation.SelectedItem = item;
        }
        finally
        {
            _syncingSelection = false;
        }
    }

    private void OnSelectionChanged(object? sender, FANavigationViewSelectionChangedEventArgs e)
    {
        if (!_syncingSelection && e.SelectedItem is FANavigationViewItem { Tag: AppPage page })
        {
            ShowPage(page);
        }
    }
}
