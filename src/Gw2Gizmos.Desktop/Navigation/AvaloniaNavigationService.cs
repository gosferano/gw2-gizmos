using Gw2Gizmos.Desktop.Core.Navigation;

namespace Gw2Gizmos.Desktop.Navigation;

/// <summary><see cref="INavigationService"/> for the Avalonia shell: forwards to the main window once it exists.</summary>
public sealed class AvaloniaNavigationService : INavigationService
{
    private MainWindow? _window;

    /// <summary>Called by the main window when it's created; navigation before that is dropped.</summary>
    public void Attach(MainWindow window) => _window = window;

    public void NavigateTo(AppPage page) => _window?.ShowPage(page);

    /// <summary>Keeps <paramref name="section"/>'s rail item highlighted while one of its sub-pages is shown; null
    /// leaves the rail's own selection.</summary>
    public void HighlightSection(AppPage? section)
    {
        if (section is { } page)
        {
            _window?.SelectRailItem(page);
        }
    }
}
