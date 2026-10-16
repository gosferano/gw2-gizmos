using System.Diagnostics;
using Gw2Gizmos.Desktop.Core.Platform;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gw2Gizmos.Desktop.Platform.Linux;

/// <summary>
/// <see cref="IAppInstallation"/> for the AppImage, which has no installer: on every start it (re)writes the launcher
/// entry (<c>$XDG_DATA_HOME/applications</c>) and icon pointing at the AppImage's current path — so moving the file
/// just repairs them — and Settings can uninstall it. Inert when not running from an AppImage (a dev build).
/// </summary>
internal sealed class AppImageInstallation : IAppInstallation, IHostedService
{
    /// <summary>The bundled launcher icon, shipped next to the app by the Linux build.</summary>
    private const string IconFileName = "gw2gizmos.png";

    /// <summary>Velopack's per-app state on Linux (staged updates), keyed by the pack id.</summary>
    private const string VelopackDirectory = "/var/tmp/velopack/Gw2Gizmos";

    private readonly string _appFolderName;
    private readonly string _dataDirectory;
    private readonly string _id;
    private readonly IStartupRegistration _startup;
    private readonly ILogger<AppImageInstallation> _logger;

    public AppImageInstallation(
        string appFolderName, string dataDirectory, IStartupRegistration startup, ILogger<AppImageInstallation> logger)
    {
        _appFolderName = appFolderName;
        _dataDirectory = dataDirectory;
        // The launcher entry and icon name: per build, like the data folder (gw2gizmos / gw2gizmos.dev).
        _id = appFolderName.ToLowerInvariant();
        _startup = startup;
        _logger = logger;
    }

    public bool CanUninstall => DesktopEntry.AppImagePath is not null;

    private string LauncherEntryPath => Path.Combine(DesktopEntry.DataHome, "applications", $"{_id}.desktop");

    private string IconPath => Path.Combine(DesktopEntry.DataHome, "icons", "hicolor", "256x256", "apps", $"{_id}.png");

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (DesktopEntry.AppImagePath is { } appImage)
        {
            try
            {
                Integrate(appImage);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Couldn't add Gw2Gizmos to the app launcher.");
            }
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public void Uninstall(bool deleteData)
    {
        string? appImage = DesktopEntry.AppImagePath;
        if (appImage is null)
        {
            throw new InvalidOperationException("Not running from an AppImage; nothing to uninstall.");
        }

        _startup.SetEnabled(false);
        DeleteQuietly(LauncherEntryPath);
        DeleteQuietly(IconPath);
        RefreshLauncherDatabase();

        if (deleteData)
        {
            // Every secret this build stored (see SecretServiceProtector's attributes).
            CommandRunner.Run("secret-tool", ["clear", "application", "gw2gizmos", "scope", _appFolderName]);
        }

        // The AppImage, Velopack's state and the data folder are in use until the app (and its worker) exit, so a
        // detached shell removes them once this process is gone.
        var paths = new List<string> { appImage, VelopackDirectory };
        if (deleteData && IsOwnDataDirectory(_dataDirectory))
        {
            paths.Add(_dataDirectory);
        }

        var cleanup = new ProcessStartInfo("sh") { UseShellExecute = false };
        cleanup.ArgumentList.Add("-c");
        cleanup.ArgumentList.Add(
            "pid=$1; shift; while kill -0 \"$pid\" 2>/dev/null; do sleep 0.5; done; sleep 1; rm -rf -- \"$@\"");
        cleanup.ArgumentList.Add("gw2gizmos-uninstall");
        cleanup.ArgumentList.Add(Environment.ProcessId.ToString());
        foreach (string path in paths)
        {
            cleanup.ArgumentList.Add(path);
        }

        Process.Start(cleanup)?.Dispose();
        _logger.LogInformation(
            "Uninstalling Gw2Gizmos ({AppImage}); {Data}.", appImage, deleteData ? "deleting its data" : "keeping its data");
    }

    private void Integrate(string appImage)
    {
        bool changed = false;

        string bundledIcon = Path.Combine(AppContext.BaseDirectory, IconFileName);
        if (File.Exists(bundledIcon) && !SameContent(bundledIcon, IconPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(IconPath)!);
            File.Copy(bundledIcon, IconPath, overwrite: true);
            changed = true;
        }

        string entry =
            "[Desktop Entry]\n"
            + "Type=Application\n"
            + "Name=Gw2Gizmos\n"
            + "GenericName=Guild Wars 2 companion\n"
            + "Comment=Account, sessions, events and trading-post prices for Guild Wars 2\n"
            + $"Exec={DesktopEntry.QuoteExec(appImage)}\n"
            + $"Icon={_id}\n"
            + "Terminal=false\n"
            + "Categories=Game;\n"
            + "Keywords=Guild Wars 2;GW2;\n"
            + $"StartupWMClass={DesktopEntry.WindowClass}\n";
        if (!File.Exists(LauncherEntryPath) || File.ReadAllText(LauncherEntryPath) != entry)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LauncherEntryPath)!);
            File.WriteAllText(LauncherEntryPath, entry);
            changed = true;
        }

        if (changed)
        {
            RefreshLauncherDatabase();
            _logger.LogInformation("Added Gw2Gizmos to the app launcher ({Entry}).", LauncherEntryPath);
        }
    }

    /// <summary>Lets launchers that cache entries pick up the change; most also watch the folder, so best effort.</summary>
    private void RefreshLauncherDatabase() =>
        CommandRunner.Run("update-desktop-database", [Path.GetDirectoryName(LauncherEntryPath)!]);

    /// <summary>Guards the recursive delete: only ever a folder named after this build, under the user's data home.</summary>
    private bool IsOwnDataDirectory(string path)
    {
        string full = Path.GetFullPath(path).TrimEnd('/');
        return Path.GetFileName(full) == _appFolderName
            && full.StartsWith(Path.GetFullPath(DesktopEntry.DataHome).TrimEnd('/') + "/", StringComparison.Ordinal);
    }

    private static bool SameContent(string a, string b) =>
        File.Exists(b) && new FileInfo(a).Length == new FileInfo(b).Length
            && File.ReadAllBytes(a).AsSpan().SequenceEqual(File.ReadAllBytes(b));

    private static void DeleteQuietly(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception)
        {
            // Already gone or not ours to delete.
        }
    }
}
