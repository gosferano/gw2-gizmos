namespace Gw2Gizmos.Desktop.Core.Platform;

/// <summary>
/// The app's own part in being installed: where it isn't installed by an installer (a Linux AppImage) it adds itself
/// to the desktop's launcher and can uninstall itself from Settings. Where an installer and the OS handle both
/// (Windows: Velopack's Setup and "Apps &amp; features") it's inert.
/// </summary>
public interface IAppInstallation
{
    /// <summary>True when Settings should offer "Uninstall": the app is responsible for removing itself.</summary>
    bool CanUninstall { get; }

    /// <summary>
    /// Removes the app: its launcher entry and launch-at-login registration now, and the app itself once it has exited;
    /// with <paramref name="deleteData"/>, also its data (database, settings, logs) and stored API keys. The caller
    /// exits the app right after.
    /// </summary>
    void Uninstall(bool deleteData);
}

/// <summary><see cref="IAppInstallation"/> where an installer manages the app (Windows): nothing to do in-app.</summary>
public sealed class InstallerManagedInstallation : IAppInstallation
{
    public bool CanUninstall => false;

    public void Uninstall(bool deleteData) =>
        throw new NotSupportedException("This app is uninstalled through the operating system.");
}
