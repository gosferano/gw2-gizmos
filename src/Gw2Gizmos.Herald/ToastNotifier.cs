using Gw2Gizmos.Data.Worker.Notifications;

namespace Gw2Gizmos.Herald;

/// <summary>
/// Bridges the engine's <see cref="INotifier"/> abstraction to real Windows toast notifications.
/// </summary>
public sealed class ToastNotifier : INotifier
{
    public void Notify(string title, string message, string category = "General") =>
        ToastService.Show(title, message);
}
