namespace Gw2Gizmos.Desktop.Core.Navigation;

/// <summary>Moves the shell to a page; implemented by each UI head.</summary>
public interface INavigationService
{
    /// <param name="reload">Re-open the page even if it's the one shown — for a page that opens on a target (a
    /// character, an item) that may have changed.</param>
    void NavigateTo(AppPage page, bool reload = false);
}
