namespace Gw2Gizmos.Data.Worker.Configuration;

/// <summary>
/// The <see cref="ISyncTriggerSource"/> for a standalone worker: there's no desktop pushing trigger generations,
/// so every sync stays at zero and the trigger watcher never fires. Standalone runs rely solely on the timer
/// cadence.
/// </summary>
public sealed class NullSyncTriggerSource : ISyncTriggerSource
{
    public long GetGeneration(string syncKey) => 0;
}
