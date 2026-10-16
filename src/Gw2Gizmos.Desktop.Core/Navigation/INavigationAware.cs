namespace Gw2Gizmos.Desktop.Core.Navigation;

/// <summary>
/// A page view-model that wants to know when the shell leaves its page — to stop live updates it subscribed to. (Not
/// <see cref="System.IDisposable"/>: the DI container would keep every disposable transient alive until exit.)
/// </summary>
public interface INavigationAware
{
    void OnNavigatedFrom();
}
