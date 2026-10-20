using Gw2Gizmos.Desktop.Core.Navigation;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// Carries the drilled-into character across the parameterless page navigation (the Characters hub → a
/// character page → its inventory), the way <see cref="SelectedAccountService"/> carries the account. It's
/// transient navigation context for the current account, so it isn't persisted.
/// </summary>
public sealed class SelectedCharacterService
{
    public string? CharacterName { get; private set; }

    public void Select(string characterName) => CharacterName = characterName;
}

/// <summary>Builds the Characters breadcrumb trails: <c>Account › Characters › ‹name›</c> and
/// <c>Account › Characters › ‹name› › Inventory</c> (characters are a section of the account).</summary>
public static class CharacterBreadcrumbs
{
    public static BreadcrumbEntry[] Character(string name) => new[]
    {
        new BreadcrumbEntry { Title = "Account", Target = AppPage.Account },
        new BreadcrumbEntry { Title = "Characters", Target = AppPage.Characters },
        new BreadcrumbEntry { Title = name },
    };

    public static BreadcrumbEntry[] Inventory(string name) => new[]
    {
        new BreadcrumbEntry { Title = "Account", Target = AppPage.Account },
        new BreadcrumbEntry { Title = "Characters", Target = AppPage.Characters },
        new BreadcrumbEntry { Title = name, Target = AppPage.Character },
        new BreadcrumbEntry { Title = "Inventory" },
    };
}
