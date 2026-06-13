using System;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// In-process event bus for notifications. <see cref="FileNotifier"/> publishes each notification as it
/// fires; the Notifications view-model subscribes to feed the in-app list. Decouples producers from the UI.
/// </summary>
public sealed class NotificationHub
{
    public event Action<Notification>? Added;

    public void Publish(Notification notification) => Added?.Invoke(notification);
}
