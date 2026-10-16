namespace Gw2Gizmos.Desktop.Core.Navigation;

/// <summary>Moves the shell to a page; implemented by each UI head.</summary>
public interface INavigationService
{
    void NavigateTo(AppPage page);
}
