using System;
using System.Collections.Generic;
using System.Linq;
using Gw2Gizmos.Data.Worker.Features;

namespace Gw2Gizmos.Data.Worker;

/// <summary>
/// The per-sync <see cref="SyncTrigger"/> registry, one entry per <see cref="WorkerSyncs"/>. The worker loops get
/// their own trigger to wait on; the <see cref="SyncTriggerWatcher"/> signals one by key when the desktop bumps
/// its generation. Singleton, so both sides share the same instances.
/// </summary>
public sealed class SyncTriggers
{
    private readonly IReadOnlyDictionary<string, SyncTrigger> _byKey =
        WorkerSyncs.All.ToDictionary(sync => sync.Key, _ => new SyncTrigger(), StringComparer.OrdinalIgnoreCase);

    public SyncTrigger Get(string syncKey) =>
        _byKey.TryGetValue(syncKey, out SyncTrigger? trigger)
            ? trigger
            : throw new ArgumentOutOfRangeException(nameof(syncKey), syncKey, "No trigger for this sync key.");
}
