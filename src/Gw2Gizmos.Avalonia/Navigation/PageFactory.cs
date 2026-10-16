using Avalonia.Controls;
using Gw2Gizmos.Avalonia.Views;
using Gw2Gizmos.Desktop.Core.Navigation;
using Microsoft.Extensions.DependencyInjection;

namespace Gw2Gizmos.Avalonia.Navigation;

/// <summary>
/// Creates the view for each <see cref="AppPage"/>, resolved from DI so its view-model is injected.
/// </summary>
public sealed class PageFactory
{
    /// <summary>Every page's view type (each registered in DI by <see cref="RegisterPages"/>).</summary>
    private static readonly Dictionary<AppPage, Type> Pages = new()
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

    private readonly IServiceProvider _services;

    public PageFactory(IServiceProvider services)
    {
        _services = services;
    }

    /// <summary>Registers every page view; transient, so each navigation gets a fresh view-model.</summary>
    public static void RegisterPages(IServiceCollection services)
    {
        foreach (Type pageType in Pages.Values)
        {
            services.AddTransient(pageType);
        }
    }

    public Control Create(AppPage page) =>
        Pages.TryGetValue(page, out Type? pageType)
            ? (Control)_services.GetRequiredService(pageType)
            : throw new InvalidOperationException($"No view is registered for page {page}.");
}
