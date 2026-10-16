using Gw2Gizmos.Desktop.Core.Navigation;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// Carries the drilled-into character across the parameterless WPF-UI navigation (the Characters hub → a
/// character page → its inventory), the way <see cref="SelectedAccountService"/> carries the account. It's
/// transient navigation context for the current account, so it isn't persisted.
/// </summary>
public sealed class SelectedCharacterService
{
    public string? CharacterName { get; private set; }

    public void Select(string characterName) => CharacterName = characterName;
}

/// <summary>Builds the Characters breadcrumb trails: <c>Characters › ‹name›</c> and
/// <c>Characters › ‹name› › Inventory</c> (the account stays global context, shown elsewhere, never a crumb).</summary>
public static class CharacterBreadcrumbs
{
    public static BreadcrumbEntry[] Character(string name) => new[]
    {
        new BreadcrumbEntry { Title = "Characters", Target = AppPage.Characters },
        new BreadcrumbEntry { Title = name },
    };

    public static BreadcrumbEntry[] Inventory(string name) => new[]
    {
        new BreadcrumbEntry { Title = "Characters", Target = AppPage.Characters },
        new BreadcrumbEntry { Title = name, Target = AppPage.Character },
        new BreadcrumbEntry { Title = "Inventory" },
    };
}
