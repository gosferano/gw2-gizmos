namespace Gw2Gizmos.Data.Worker.Notifications;

/// <summary>
/// Surfaces a user-facing notification. The detection logic depends only on this abstraction, so
/// the display mechanism (log line today, a Windows toast from the future tray app) can be swapped
/// without touching the change-detection code.
/// </summary>
public interface INotifier
{
    /// <param name="category">Groups the notification for filtering/display (e.g. "Delivery", "Account").</param>
    void Notify(string title, string message, string category = "General");

    /// <summary>
    /// As <see cref="Notify(string,string,string)"/>, but the notification offers a button that copies
    /// <paramref name="copyText"/> to the clipboard (e.g. an event's waypoint chat link on a desktop toast).
    /// Notifiers without an actionable surface fall back to a plain notification.
    /// </summary>
    /// <param name="copyText">Text the copy button places on the clipboard.</param>
    /// <param name="copyLabel">Label shown on the copy button.</param>
    void Notify(string title, string message, string category, string copyText, string copyLabel)
        => Notify(title, message, category);
}
