using System;
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

        // Navigate once the control's visual tree is ready (navigating in the ctor NREs). First launch lands on the
        // one-time welcome/onboarding page; afterwards (and on every later launch) straight to the dashboard.
        Type landing = onboarding.IsCompleted ? typeof(DashboardPage) : typeof(OnboardingPage);
        Loaded += (_, _) => RootNavigation.Navigate(landing);
    }
}
