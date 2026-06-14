namespace Gw2Gizmos.Data.Worker.Configuration;

/// <summary>
/// Supplies the current trigger generation for each worker sync (see <see cref="Features.WorkerSyncs"/>). The
/// desktop bumps a sync's generation when the user enables its feature or adds a key; a worker that sees the
/// value increase runs that sync immediately rather than waiting for its next timer tick. Monotonic per session,
/// so the worker only needs to compare against the last value it acted on. Standalone workers have no desktop to
/// signal them, so they use a no-op source (see <see cref="NullSyncTriggerSource"/>).
/// </summary>
public interface ISyncTriggerSource
{
    /// <summary>The current generation for the sync; higher than the last observed value means "run now". Zero
    /// when the source has no value for the key.</summary>
    long GetGeneration(string syncKey);
}
