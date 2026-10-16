namespace Gw2Gizmos.Desktop.Core.Platform;

/// <summary>
/// The per-user "launch at login" registration. When enabled the OS starts the app with <c>--minimized</c> so it
/// comes up in the tray. Best-effort: a registration that can't be written just doesn't persist, never a crash.
/// </summary>
public interface IStartupRegistration
{
    bool IsEnabled { get; }

    void SetEnabled(bool enabled);
}
