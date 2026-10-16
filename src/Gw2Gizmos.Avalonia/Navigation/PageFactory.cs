using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Layout;
using Gw2Gizmos.Avalonia.Controls;
using Gw2Gizmos.Avalonia.Views;
using Gw2Gizmos.Desktop.Core.Navigation;
using Microsoft.Extensions.DependencyInjection;

namespace Gw2Gizmos.Avalonia.Navigation;

/// <summary>
/// Creates the view for each <see cref="AppPage"/>, resolved from DI so its view-model is injected. Pages are ported
/// one at a time; until a page exists it shows a placeholder in the standard page chrome.
/// </summary>
public sealed partial class PageFactory
{
    /// <summary>The ported pages' view types (each registered in DI by <see cref="RegisterPages"/>).</summary>
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

    /// <summary>Registers every ported page view; transient, so each navigation gets a fresh view-model.</summary>
    public static void RegisterPages(IServiceCollection services)
    {
        foreach (Type pageType in Pages.Values)
        {
            services.AddTransient(pageType);
        }
    }

    // The section each not-yet-ported sub-page sits under, so its placeholder shows the real breadcrumb trail.
    private static readonly Dictionary<AppPage, AppPage> PlaceholderParents = new()
    {
        [AppPage.Session] = AppPage.Sessions,
        [AppPage.SessionLoot] = AppPage.Sessions,
    };

    public Control Create(AppPage page) =>
        Pages.TryGetValue(page, out Type? pageType)
            ? (Control)_services.GetRequiredService(pageType)
            : Placeholder(page);

    private static PageShell Placeholder(AppPage page) => new()
    {
        Title = DisplayName(page),
        Breadcrumbs = PlaceholderParents.TryGetValue(page, out AppPage parent)
            ? new[]
            {
                new BreadcrumbEntry { Title = DisplayName(parent), Target = parent },
                new BreadcrumbEntry { Title = DisplayName(page) },
            }
            : null,
        Content = new Card
        {
            VerticalAlignment = VerticalAlignment.Top,
            Content = new TextBlock { Text = "Not ported to Avalonia yet.", Opacity = 0.7 },
        },
    };

    // "MaterialStorage" → "Material Storage".
    private static string DisplayName(AppPage page) => SplitWords().Replace(page.ToString(), " $1");

    [GeneratedRegex("(?<=[a-z])([A-Z])")]
    private static partial Regex SplitWords();
}
