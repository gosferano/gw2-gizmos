using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using Gw2Gizmos.Desktop.Navigation;
using Gw2Gizmos.Desktop.Core;
using Gw2Gizmos.Desktop.Core.Navigation;

namespace Gw2Gizmos.Desktop;

/// <summary>The app shell: a Fluent navigation rail whose items open pages created by the <see cref="PageFactory"/>.</summary>
public partial class MainWindow : Window
{
    private readonly PageFactory? _pages;
    private readonly GlobalSearchViewModel? _search;
    private readonly NavigationHistory? _history;
    private AppPage? _current;
    private bool _syncingSelection;
    private bool _movingThroughHistory;

    /// <summary>Parameterless constructor for the XAML previewer.</summary>
    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(
        AvaloniaNavigationService navigation,
        PageFactory pages,
        OnboardingStore onboarding,
        GlobalSearchViewModel search,
        NavigationHistory history)
        : this()
    {
        _pages = pages;
        _search = search;
        _history = history;
        history.Changed += (_, _) =>
        {
            BackButton.IsEnabled = history.CanGoBack;
            ForwardButton.IsEnabled = history.CanGoForward;
        };
        navigation.Attach(this);
        RootNavigation.SelectionChanged += OnSelectionChanged;
        RootNavigation.ItemInvoked += OnItemInvoked;
        SearchOverlay.DataContext = search;
        SearchOverlay.SizeChanged += (_, _) => PlaceSearchCard();
        // Ctrl+K from anywhere in the window, even while a text box has focus.
        AddHandler(KeyDownEvent, OnWindowKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        // The mouse's side buttons go back and forward from anywhere, even over a control that handles the press.
        AddHandler(PointerPressedEvent, OnWindowPointerPressed, handledEventsToo: true);

        // First launch lands on the one-time welcome/onboarding page; afterwards straight to the dashboard.
        ShowPage(StartPageOverride() ?? (onboarding.IsCompleted ? AppPage.Dashboard : AppPage.Onboarding));
    }

    /// <summary>Debug builds: <c>--start-page=Wallet</c> opens that page directly (handy while working on a page).</summary>
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

    /// <summary>Shows <paramref name="page"/> and highlights its rail item (if it has one); <paramref name="reload"/>
    /// re-creates it even when it's already shown.</summary>
    public void ShowPage(AppPage page, bool reload = false)
    {
        if (_current == page && !reload)
        {
            return;
        }

        _current = page;
        // Recorded before the page is created, so its target (an item to select) is captured before it takes it.
        if (!_movingThroughHistory)
        {
            _history?.Record(page);
        }

        // Let the page being left stop its live updates.
        ((RootNavigation.Content as Control)?.DataContext as INavigationAware)?.OnNavigatedFrom();
        RootNavigation.Content = _pages?.Create(page);
        ((RootNavigation.Content as Control)?.DataContext as INavigationAware)?.OnNavigatedTo();

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

    private void OnItemInvoked(object? sender, FANavigationViewItemInvokedEventArgs e)
    {
        if (e.InvokedItemContainer is FANavigationViewItem { Tag: "search" })
        {
            OpenSearch();
        }
    }

    /// <summary>Goes back to the previous page visited, with what it showed.</summary>
    public void GoBack() => MoveThroughHistory(_history?.Back());

    /// <summary>Goes forward again, after going back.</summary>
    public void GoForward() => MoveThroughHistory(_history?.Forward());

    private void MoveThroughHistory(AppPage? page)
    {
        if (page is not { } target)
        {
            return;
        }

        _search?.Close();
        _movingThroughHistory = true;
        try
        {
            // Reopened even when it's the page shown: the same page can show something else (another character).
            ShowPage(target, reload: true);
        }
        finally
        {
            _movingThroughHistory = false;
        }
    }

    private void OnBackClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => GoBack();

    private void OnForwardClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => GoForward();

    private void OnWindowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        switch (e.GetCurrentPoint(this).Properties.PointerUpdateKind)
        {
            case PointerUpdateKind.XButton1Pressed:
                e.Handled = true;
                GoBack();
                break;
            case PointerUpdateKind.XButton2Pressed:
                e.Handled = true;
                GoForward();
                break;
        }
    }

    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyModifiers == KeyModifiers.Alt && e.Key is Key.Left or Key.Right)
        {
            e.Handled = true;
            if (e.Key == Key.Left)
            {
                GoBack();
            }
            else
            {
                GoForward();
            }
        }
        else if (e.Key == Key.K && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            e.Handled = true;
            if (_search is { IsOpen: true })
            {
                _search.Close();
            }
            else
            {
                OpenSearch();
            }
        }
        else if (e.Key == Key.Escape && _search is { IsOpen: true })
        {
            e.Handled = true;
            _search.Close();
        }
    }

    private void OpenSearch()
    {
        if (_search is null)
        {
            return;
        }

        _ = _search.OpenAsync();
        // Focus once the overlay is visible.
        Dispatcher.UIThread.Post(() => SearchBox.Focus(), DispatcherPriority.Input);
        Dispatcher.UIThread.Post(PlaceSearchCard, DispatcherPriority.Loaded);
    }

    /// <summary>
    /// Places the search card so it's centred at its full height (as it opens, listing the pages), then leaves its top
    /// there: as the matches narrow the card shrinks from the bottom, so the box being typed in stays put.
    /// </summary>
    private void PlaceSearchCard()
    {
        if (!SearchOverlay.IsVisible)
        {
            return;
        }

        double fullHeight = SearchCard.Padding.Top + SearchCard.Padding.Bottom
            + SearchCard.BorderThickness.Top + SearchCard.BorderThickness.Bottom
            + SearchBox.Height + SearchResults.Margin.Top + SearchResults.MaxHeight;
        double top = Math.Max(16, (SearchOverlay.Bounds.Height - fullHeight) / 2);
        Thickness margin = SearchCard.Margin;
        SearchCard.Margin = new Thickness(margin.Left, top, margin.Right, margin.Bottom);
    }

    /// <summary>Arrow keys move through the results and Enter opens one, while typing stays in the box.</summary>
    private void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Down:
                _search?.MoveSelection(1);
                break;
            case Key.Up:
                _search?.MoveSelection(-1);
                break;
            case Key.Enter:
                _search?.Activate();
                break;
            default:
                return;
        }

        e.Handled = true;
        if (_search?.SelectedResult is { } selected)
        {
            SearchResults.ScrollIntoView(selected);
        }
    }

    private void OnSearchResultTapped(object? sender, TappedEventArgs e)
    {
        if (e.Source is Control { DataContext: SearchResult result })
        {
            _search?.Activate(result);
        }
    }

    private void OnSearchBackdropPressed(object? sender, PointerPressedEventArgs e) => _search?.Close();

    private void OnSelectionChanged(object? sender, FANavigationViewSelectionChangedEventArgs e)
    {
        if (!_syncingSelection && e.SelectedItem is FANavigationViewItem { Tag: AppPage page })
        {
            ShowPage(page);
        }
    }
}
