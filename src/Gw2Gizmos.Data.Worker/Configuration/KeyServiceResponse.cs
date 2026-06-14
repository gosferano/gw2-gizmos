namespace Gw2Gizmos.Data.Worker.Configuration;

/// <summary>
/// Payload the desktop's key service returns over the local pipe. A list, so supporting several keys
/// (one per account) later needs no wire change; the current build sends a single key.
/// </summary>
public sealed class KeyServiceResponse
{
    public string[] Keys { get; set; } = System.Array.Empty<string>();

    /// <summary>
    /// The feature keys the user has enabled on the desktop's Settings page (see <see cref="IFeatureGate"/>);
    /// the worker runs only these. The worker (whose lifetime is bounded by the desktop that spawned it) always
    /// gets the live set with the keys in the same payload, so there's no "unknown" state to default.
    /// </summary>
    public string[] EnabledFeatures { get; set; } = System.Array.Empty<string>();
}
