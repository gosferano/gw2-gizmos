using Gw2Gizmos.Data.Worker.Notifications;
using Gw2Gizmos.Desktop.Core.Platform;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// The desktop's <see cref="INotifier"/>: fires an OS notification (a Windows toast) unless the user has
/// disabled that category on the Notifications settings page. OS notifications are the only surface — there is
/// no in-app feed.
/// </summary>
public sealed class NotificationDispatcher : INotifier
{
    private readonly NotificationSettingsStore _settings;
    private readonly IToastService _toasts;

    public NotificationDispatcher(NotificationSettingsStore settings, IToastService toasts)
    {
        _settings = settings;
        _toasts = toasts;
    }

    public void Notify(string title, string message, string category = "General")
    {
        if (!_settings.IsEnabled(category))
        {
            return;
        }

        _toasts.Show(title, message);
    }

    public void Notify(string title, string message, string category, string copyText, string copyLabel)
    {
        if (!_settings.IsEnabled(category))
        {
            return;
        }

        _toasts.Show(title, message, copyText, copyLabel);
    }
}
