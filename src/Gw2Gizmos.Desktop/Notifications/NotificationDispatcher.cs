using Gw2Gizmos.Data.Worker.Notifications;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// The desktop's <see cref="INotifier"/>: fires a Windows toast for a notification unless the user has
/// disabled that category on the Notifications settings page. Toasts are the only surface — there is no
/// in-app feed.
/// </summary>
public sealed class NotificationDispatcher : INotifier
{
    private readonly NotificationSettingsStore _settings;

    public NotificationDispatcher(NotificationSettingsStore settings)
    {
        _settings = settings;
    }

    public void Notify(string title, string message, string category = "General")
    {
        if (!_settings.IsEnabled(category))
        {
            return;
        }

        ToastService.Show(title, message);
    }

    public void Notify(string title, string message, string category, string copyText, string copyLabel)
    {
        if (!_settings.IsEnabled(category))
        {
            return;
        }

        ToastService.Show(title, message, copyText, copyLabel);
    }
}
