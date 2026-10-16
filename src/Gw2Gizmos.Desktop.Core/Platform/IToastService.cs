namespace Gw2Gizmos.Desktop.Core.Platform;

/// <summary>OS-level notifications (Windows toasts, freedesktop notifications on Linux).</summary>
public interface IToastService
{
    /// <param name="copyText">When set, the notification carries an action button that copies this text to the
    /// clipboard. Null shows a plain notification.</param>
    /// <param name="copyLabel">Label for that button; defaults to "Copy".</param>
    void Show(string title, string body, string? copyText = null, string? copyLabel = null);
}
