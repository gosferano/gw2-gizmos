using System;
using Velopack;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// Shared app-update state. The startup update check (in <c>App</c>) downloads any new release and, once it's
/// staged, records it here; the dashboard reads it to show an "update ready" hint and lets the user apply it.
/// Velopack otherwise applies the staged update on the next restart anyway.
/// </summary>
public sealed class UpdateStatus
{
    private UpdateManager? _manager;
    private UpdateInfo? _pending;

    /// <summary>The version of the downloaded-and-staged update awaiting a restart, or null when up to date.</summary>
    public string? PendingVersion { get; private set; }

    public bool UpdateReady => _pending is not null;

    /// <summary>Raised when an update becomes ready, so an open view can refresh.</summary>
    public event Action? Changed;

    public void SetPending(UpdateManager manager, UpdateInfo update)
    {
        _manager = manager;
        _pending = update;
        PendingVersion = update.TargetFullRelease.Version.ToString();
        Changed?.Invoke();
    }

    /// <summary>Applies the staged update and restarts the app (no-op if nothing is pending). Exits the process.</summary>
    public void ApplyAndRestart()
    {
        if (_manager is not null && _pending is not null)
        {
            _manager.ApplyUpdatesAndRestart(_pending);
        }
    }
}
