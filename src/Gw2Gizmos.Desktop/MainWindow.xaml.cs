using System;
using System.Windows.Input;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Controls;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// The app shell: a Fluent window with a navigation rail. Each feature is a page resolved from DI
/// via the <see cref="INavigationViewPageProvider"/>; new features are new pages.
/// </summary>
public partial class MainWindow : FluentWindow
{
    public MainWindow(INavigationViewPageProvider pageProvider, OnboardingStore onboarding)
    {
        InitializeComponent();
        RootNavigation.SetPageProviderService(pageProvider);

        // Expose the nav view so Account cards + breadcrumbs can navigate to sub-pages.
        App.MainNavigation = RootNavigation;

        // Back navigation via Alt+Left and the mouse "back" button (no on-screen button). WPF-UI journals every
        // Navigate; App snapshots the selection per page (tracked on Navigated) and restores it on back.
        RootNavigation.AddHandler(NavigationView.NavigatedEvent,
            new System.Windows.RoutedEventHandler((_, _) => App.OnNavigated()));
        PreviewKeyDown += OnPreviewKeyDown;
        PreviewMouseDown += OnPreviewMouseDown;

        // Navigate once the control's visual tree is ready (navigating in the ctor NREs). First launch lands on the
        // one-time welcome/onboarding page; afterwards (and on every later launch) straight to the dashboard.
        Type landing = onboarding.IsCompleted ? typeof(DashboardPage) : typeof(OnboardingPage);
        Loaded += (_, _) => RootNavigation.Navigate(landing);
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        // With Alt held, the key arrives as a system key, so read SystemKey in that case.
        Key key = e.Key == Key.System ? e.SystemKey : e.Key;
        bool isBack = key == Key.BrowserBack
            || (key == Key.Left && (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt);
        if (isBack && RootNavigation.CanGoBack)
        {
            App.GoBack();
            e.Handled = true;
        }
    }

    private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.XButton1 && RootNavigation.CanGoBack)
        {
            App.GoBack();
            e.Handled = true;
        }
    }
}
