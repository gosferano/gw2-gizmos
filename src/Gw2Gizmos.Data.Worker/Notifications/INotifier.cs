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
}
