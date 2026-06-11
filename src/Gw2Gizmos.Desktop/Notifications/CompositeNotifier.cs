using Gw2Gizmos.Data.Worker.Notifications;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// Fans a notification out to several single-purpose <see cref="INotifier"/> implementations
/// (e.g. persist to the DB, fire a Windows toast). The engine depends on one <see cref="INotifier"/>;
/// this is what it gets.
/// </summary>
public sealed class CompositeNotifier : INotifier
{
    private readonly INotifier[] _notifiers;

    public CompositeNotifier(params INotifier[] notifiers)
    {
        _notifiers = notifiers;
    }

    public void Notify(string title, string message, string category = "General")
    {
        foreach (INotifier notifier in _notifiers)
        {
            notifier.Notify(title, message, category);
        }
    }
}
