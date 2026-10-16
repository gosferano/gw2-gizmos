using System;
using System.Collections.Generic;
using Gw2Gizmos.Desktop.Core.Navigation;

namespace Gw2Gizmos.Desktop.Ui;

/// <summary>
/// <see cref="INavigationService"/> for the WPF shell: maps each <see cref="AppPage"/> onto its WPF page type and
/// navigates the window's NavigationView there.
/// </summary>
public sealed class WpfNavigationService : INavigationService
{
    private static readonly Dictionary<AppPage, Type> PageTypes = new()
    {
        [AppPage.Onboarding] = typeof(OnboardingPage),
        [AppPage.Dashboard] = typeof(DashboardPage),
        [AppPage.Account] = typeof(AccountPage),
        [AppPage.Wallet] = typeof(WalletPage),
        [AppPage.MaterialStorage] = typeof(MaterialStoragePage),
        [AppPage.Bank] = typeof(BankPage),
        [AppPage.SharedInventory] = typeof(SharedInventoryPage),
        [AppPage.Characters] = typeof(CharactersPage),
        [AppPage.Character] = typeof(CharacterPage),
        [AppPage.CharacterInventory] = typeof(CharacterInventoryPage),
        [AppPage.Sessions] = typeof(SessionsPage),
        [AppPage.Session] = typeof(SessionPage),
        [AppPage.SessionLoot] = typeof(SessionLootPage),
        [AppPage.Events] = typeof(EventsPage),
        [AppPage.Items] = typeof(ItemsPage),
        [AppPage.Notifications] = typeof(NotificationsPage),
        [AppPage.Logs] = typeof(LogsPage),
        [AppPage.ApiKeys] = typeof(ApiKeysPage),
        [AppPage.Settings] = typeof(SettingsPage),
        [AppPage.AdvancedSettings] = typeof(AdvancedSettingsPage),
        [AppPage.StoredData] = typeof(StoredDataPage),
    };

    /// <summary>The WPF page type that renders <paramref name="page"/>.</summary>
    public static Type PageTypeFor(AppPage page) => PageTypes[page];

    public void NavigateTo(AppPage page) => App.NavigateTo(PageTypeFor(page));
}
