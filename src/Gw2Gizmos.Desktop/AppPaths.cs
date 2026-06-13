using System.IO;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// The desktop's per-user data directory and a helper for resolving file paths within it. The app keeps its
/// own state (API key, event subscriptions/favorites, reminder settings, notification history, delivery
/// baseline) here as files — the ingestion database is owned exclusively by the worker and opened read-only.
/// </summary>
public sealed class AppPaths
{
    public AppPaths(string dataDirectory)
    {
        DataDirectory = dataDirectory;
        Directory.CreateDirectory(dataDirectory);
    }

    public string DataDirectory { get; }

    /// <summary>Absolute path to <paramref name="fileName"/> inside the data directory.</summary>
    public string File(string fileName) => Path.Combine(DataDirectory, fileName);
}
