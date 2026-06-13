using Wpf.Ui.Abstractions;
using Wpf.Ui.Controls;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// The app shell: a Fluent window with a navigation rail. Each feature is a page resolved from DI
/// via the <see cref="INavigationViewPageProvider"/>; new features are new pages.
/// </summary>
public partial class MainWindow : FluentWindow
{
    public MainWindow(INavigationViewPageProvider pageProvider)
    {
        InitializeComponent();
        RootNavigation.SetPageProviderService(pageProvider);

        // Expose the nav view so Account cards + breadcrumbs can navigate to sub-pages.
        App.MainNavigation = RootNavigation;

        // Navigate once the control's visual tree is ready (navigating in the ctor NREs).
        Loaded += (_, _) => RootNavigation.Navigate(typeof(NotificationsPage));
    }
}
