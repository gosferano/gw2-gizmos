namespace Gw2Gizmos.Data.Worker;

/// <summary>
/// Serializes account syncs across the worker so the periodic account loop and the on-demand session-boundary
/// syncs never write the account observation tables at the same time — concurrent writers on SQLite risk
/// "database is locked" and duplicate observation rows. A process-wide singleton shared by <see cref="Worker"/>
/// and the session tracker.
/// </summary>
public sealed class AccountSyncGate
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task RunExclusivelyAsync(Func<Task> action, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await action();
        }
        finally
        {
            _gate.Release();
        }
    }
}
