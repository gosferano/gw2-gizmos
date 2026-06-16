namespace Gw2Gizmos.Data.Worker.Configuration;

/// <summary>
/// A one-shot request from the desktop for the worker to delete a category of stored data (see
/// <see cref="Features.DeletableData"/>). Rides the config pipe payload; the worker runs each id it hasn't seen
/// yet. In-memory on the desktop (the worker's lifetime is bounded by it), so a processed request never replays
/// after a restart and re-wipes freshly-synced data.
/// </summary>
public sealed class DeleteRequest
{
    /// <summary>Monotonic id; the worker tracks the highest it has processed.</summary>
    public long Id { get; set; }

    /// <summary>The <see cref="Features.DeletableData"/> category key.</summary>
    public string TypeKey { get; set; } = "";

    /// <summary>The target account, or null for a global (non-account) category.</summary>
    public string? AccountId { get; set; }
}
