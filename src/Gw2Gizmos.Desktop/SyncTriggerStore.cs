using System;
using System.Collections.Generic;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// Holds a monotonic per-sync trigger generation that the desktop bumps when the user enables a feature or adds a
/// key, and the <see cref="WorkerConfigHost"/> ships to the worker over the config pipe. The worker runs a sync
/// immediately when it sees its generation increase, instead of waiting for the next timer tick. In-memory only
/// (resets each app run, which is fine: the worker re-seeds its baseline on the matching restart).
/// </summary>
public sealed class SyncTriggerStore
{
    private readonly object _gate = new();
    private readonly Dictionary<string, long> _generations = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Bumps the sync's generation, asking the worker to run it as soon as it next polls (~a few seconds).</summary>
    public void Bump(string syncKey)
    {
        lock (_gate)
        {
            _generations.TryGetValue(syncKey, out long current);
            _generations[syncKey] = current + 1;
        }
    }

    /// <summary>The current generation map, copied for the pipe payload.</summary>
    public Dictionary<string, long> Snapshot()
    {
        lock (_gate)
        {
            return new Dictionary<string, long>(_generations, StringComparer.OrdinalIgnoreCase);
        }
    }
}
