using System;
using System.Collections.Generic;

namespace Gw2Gizmos.Data.Worker.Configuration;

/// <summary>
/// The desktop's live worker configuration, served to the worker over a same-machine named pipe: the API keys,
/// the enabled features, and the per-sync intervals. The worker (whose lifetime is bounded by the desktop that
/// spawned it) gets the whole set in one payload, so there's no "unknown" state to default.
/// </summary>
public sealed class WorkerConfigPayload
{
    /// <summary>The raw GW2 API keys, one per account (see <see cref="IGw2ApiKeyProvider"/>).</summary>
    public string[] Keys { get; set; } = System.Array.Empty<string>();

    /// <summary>The feature keys the user has enabled (see <see cref="IFeatureGate"/>); the worker runs only these.</summary>
    public string[] EnabledFeatures { get; set; } = System.Array.Empty<string>();

    /// <summary>Per-sync cadence, keyed by <see cref="Features.WorkerSyncs"/> key (see
    /// <see cref="IIntervalGate"/>). An absent key falls back to the catalog default.</summary>
    public Dictionary<string, TimeSpan> Intervals { get; set; } = new();

    /// <summary>Per-sync trigger generation, keyed by <see cref="Features.WorkerSyncs"/> key (see
    /// <see cref="ISyncTriggerSource"/>). The desktop bumps a sync's value when the user enables its feature or
    /// adds a key; the worker runs that sync immediately when it sees the value increase. Monotonic; an absent
    /// key means zero.</summary>
    public Dictionary<string, long> SyncGenerations { get; set; } = new();
}
