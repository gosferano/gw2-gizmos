using System;

namespace Gw2Gizmos.Data.Worker.Configuration;

/// <summary>
/// Supplies the sync cadence for each worker sync (see <see cref="Features.WorkerSyncs"/>). The source is
/// host-specific: the desktop's Advanced settings (pushed over the config pipe) when desktop-launched, or
/// <c>Worker:Intervals:&lt;Key&gt;</c> configuration for a standalone worker. Read live, so a changed interval
/// takes effect on the next timer wait without a restart.
/// </summary>
public interface IIntervalGate
{
    /// <summary>The configured cadence for the sync, or its catalog default.</summary>
    TimeSpan GetInterval(string syncKey);
}
