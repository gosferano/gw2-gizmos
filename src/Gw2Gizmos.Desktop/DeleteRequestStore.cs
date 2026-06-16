using System.Collections.Generic;
using Gw2Gizmos.Data.Worker.Configuration;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// Queues the user's "delete stored data" requests for the worker to run (the worker is the DB's sole writer).
/// The <see cref="WorkerConfigHost"/> ships the queue over the config pipe; the worker executes each id once.
/// In-memory only (resets each app run) — ids are monotonic, so a processed request never replays after a
/// restart and re-wipes data that synced in between.
/// </summary>
public sealed class DeleteRequestStore
{
    private readonly object _gate = new();
    private readonly List<DeleteRequest> _requests = new();
    private long _nextId;

    /// <summary>Queues a delete of <paramref name="typeKey"/> for an account (or null for global data).</summary>
    public void Enqueue(string typeKey, string? accountId)
    {
        lock (_gate)
        {
            _requests.Add(new DeleteRequest { Id = ++_nextId, TypeKey = typeKey, AccountId = accountId });
        }
    }

    /// <summary>The queued requests, copied for the pipe payload.</summary>
    public DeleteRequest[] Snapshot()
    {
        lock (_gate)
        {
            return _requests.ToArray();
        }
    }
}
