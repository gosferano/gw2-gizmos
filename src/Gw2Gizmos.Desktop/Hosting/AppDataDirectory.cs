namespace Gw2Gizmos.Desktop.Hosting;

/// <summary>Where this app keeps its per-user state (settings, key file, logs, the worker-owned database).</summary>
internal static class AppDataDirectory
{
    // A development build (Debug) keeps its own data folder and single-instance guard so it can run side by side
    // with an installed release without sharing its database.
#if DEBUG
    public const string FolderName = "Gw2Gizmos.Dev";
#else
    public const string FolderName = "Gw2Gizmos";
#endif

    /// <summary>
    /// Creates (if needed) and returns the data directory: <c>%APPDATA%\Gw2Gizmos</c> on Windows (shared with the WPF
    /// app), <c>$XDG_DATA_HOME/Gw2Gizmos</c> (usually <c>~/.local/share</c>) elsewhere. On Unix it is restricted to
    /// the owner, since it holds the API-key file and the database.
    /// </summary>
    public static string Ensure()
    {
        Environment.SpecialFolder root = OperatingSystem.IsWindows()
            ? Environment.SpecialFolder.ApplicationData
            : Environment.SpecialFolder.LocalApplicationData;
        string path = Path.Combine(Environment.GetFolderPath(root), FolderName);

        if (OperatingSystem.IsWindows())
        {
            Directory.CreateDirectory(path);
        }
        else
        {
            const UnixFileMode OwnerOnly = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;
            Directory.CreateDirectory(path, OwnerOnly);
            // Also tighten a directory that already existed with looser permissions.
            File.SetUnixFileMode(path, OwnerOnly);
        }

        return path;
    }
}
