using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Velopack;
using Velopack.Sources;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// Self-update via Velopack: checks for a new release when the app starts and then on an interval, so a release
/// published while the app is running surfaces in-app (the dashboard reacts to <see cref="UpdateStatus"/>) without a
/// restart. A found update is downloaded and staged; Velopack applies it on the next restart (or the dashboard's
/// "Restart now"). No-op for a dev build run from bin, which isn't Velopack-installed.
/// </summary>
public sealed class AppUpdateService : BackgroundService
{
    /// <summary>How often to re-check for updates while the app is running.</summary>
    private static readonly TimeSpan UpdateCheckInterval = TimeSpan.FromMinutes(30);

    private readonly UpdateStatus _updateStatus;
    private readonly ILogger<AppUpdateService> _logger;

    // Read the feed as a plain release *asset* via GitHub's stable /releases/latest/download URL (a CDN redirect),
    // NOT GithubSource — whose REST release enumeration is throttled to 60 req/hr per IP and 403s ("rate limit
    // exceeded") behind shared/CGNAT addresses, which is what broke the update check. CI always packs full-only, so
    // the feed at "latest" only references that release's own assets.
    private readonly UpdateManager _updateManager =
        new(new SimpleWebSource("https://github.com/gosferano/gw2-gizmos/releases/latest/download"));

    public AppUpdateService(UpdateStatus updateStatus, ILogger<AppUpdateService> logger)
    {
        _updateStatus = updateStatus;
        _logger = logger;
    }

    /// <summary>True for a Velopack-installed build, false for a dev/bin run; never throws.</summary>
    public bool IsInstalled
    {
        get
        {
            try
            {
                return _updateManager.IsInstalled;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>Checks immediately, then every <see cref="UpdateCheckInterval"/>. Stops once an update is staged —
    /// a pending update doesn't change until the next launch applies it.</summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(UpdateCheckInterval);
        try
        {
            do
            {
                await CheckForUpdatesAsync(stoppingToken);
                if (_updateStatus.UpdateReady)
                {
                    return;
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // Host is shutting down.
        }
    }

    private async Task CheckForUpdatesAsync(CancellationToken stoppingToken)
    {
        try
        {
            if (!_updateManager.IsInstalled)
            {
                _logger.LogInformation("Update check skipped: not a Velopack install (dev/bin run).");
                return;
            }

            _logger.LogInformation("Checking for updates (current version {Version})…", _updateManager.CurrentVersion);
            UpdateInfo? update = await _updateManager.CheckForUpdatesAsync();
            if (update is null)
            {
                _logger.LogInformation("Update check: no newer release available.");
                return;
            }

            _logger.LogInformation("Update {Version} found; downloading…", update.TargetFullRelease.Version);
            // Download now; Velopack applies it on the next restart so the session isn't interrupted.
            await _updateManager.DownloadUpdatesAsync(update, cancelToken: stoppingToken);
            // Surface it in the UI (the dashboard's App card) — the staged update applies on next restart, or
            // immediately via the card's "Restart now" button.
            _updateStatus.SetPending(_updateManager, update);
            _logger.LogInformation("Update {Version} downloaded and staged.", update.TargetFullRelease.Version);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Update check failed.");
        }
    }
}
