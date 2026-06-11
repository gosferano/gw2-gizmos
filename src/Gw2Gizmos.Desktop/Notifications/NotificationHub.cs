using System;
using Gw2Gizmos.Data.EntityFramework.Entities.Notifications;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// In-process event bus for notifications. The DB notifier publishes the app's own notifications and
/// the cross-process watcher publishes the worker's; the Notifications view-model subscribes to feed
/// the in-app list. Decouples producers from the UI.
/// </summary>
public sealed class NotificationHub
{
    public event Action<Notification>? Added;

    public void Publish(Notification notification) => Added?.Invoke(notification);
}
