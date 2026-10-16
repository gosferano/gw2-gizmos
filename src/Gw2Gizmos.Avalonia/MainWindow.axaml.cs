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
        ShowPage(onboarding.IsCompleted ? AppPage.Dashboard : AppPage.Onboarding);
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

        // Keep the rail in sync when navigation comes from code (the guard above stops the resulting
        // SelectionChanged from navigating again). Pages without their own rail item leave the selection as is.
        FANavigationViewItem? item = RootNavigation.MenuItems
            .Concat(RootNavigation.FooterMenuItems)
            .OfType<FANavigationViewItem>()
            .FirstOrDefault(candidate => candidate.Tag is AppPage tag && tag == page);
        if (item is not null)
        {
            RootNavigation.SelectedItem = item;
        }
    }

    private void OnSelectionChanged(object? sender, FANavigationViewSelectionChangedEventArgs e)
    {
        if (e.SelectedItem is FANavigationViewItem { Tag: AppPage page })
        {
            ShowPage(page);
        }
    }
}
