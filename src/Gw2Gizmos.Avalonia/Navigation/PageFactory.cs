using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Layout;
using Gw2Gizmos.Avalonia.Controls;
using Gw2Gizmos.Desktop.Core.Navigation;

namespace Gw2Gizmos.Avalonia.Navigation;

/// <summary>
/// Creates the view for each <see cref="AppPage"/>. Pages are ported one at a time; until a page exists it shows a
/// placeholder in the standard page chrome.
/// </summary>
public sealed partial class PageFactory
{
    // The section each not-yet-ported sub-page sits under, so its placeholder shows the real breadcrumb trail.
    private static readonly Dictionary<AppPage, AppPage> PlaceholderParents = new()
    {
        [AppPage.Wallet] = AppPage.Account,
        [AppPage.MaterialStorage] = AppPage.Account,
        [AppPage.Bank] = AppPage.Account,
        [AppPage.SharedInventory] = AppPage.Account,
        [AppPage.Character] = AppPage.Characters,
        [AppPage.CharacterInventory] = AppPage.Characters,
        [AppPage.Session] = AppPage.Sessions,
        [AppPage.SessionLoot] = AppPage.Sessions,
        [AppPage.AdvancedSettings] = AppPage.Settings,
        [AppPage.StoredData] = AppPage.Settings,
    };

    public Control Create(AppPage page) => Placeholder(page);

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
