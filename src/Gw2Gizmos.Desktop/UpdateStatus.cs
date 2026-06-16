using System;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// Shared app-update state. The startup update check (in <c>App</c>) downloads any new release and, once it's
/// staged, records the pending version here; the dashboard reads it to show an "update ready" hint. Velopack
/// applies the staged update on the next restart, so there's nothing to do but tell the user.
/// </summary>
public sealed class UpdateStatus
{
    /// <summary>The version of the downloaded-and-staged update awaiting a restart, or null when up to date.</summary>
    public string? PendingVersion { get; private set; }

    public bool UpdateReady => PendingVersion is not null;

    /// <summary>Raised when an update becomes ready, so an open view can refresh.</summary>
    public event Action? Changed;

    public void SetPending(string version)
    {
        PendingVersion = version;
        Changed?.Invoke();
    }
}
