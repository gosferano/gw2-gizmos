namespace Gw2Gizmos.Data.Worker.Configuration;

/// <summary>
/// Payload the desktop's key service returns over the local pipe. A list, so supporting several keys
/// (one per account) later needs no wire change; the current build sends a single key.
/// </summary>
public sealed class KeyServiceResponse
{
    public string[] Keys { get; set; } = System.Array.Empty<string>();
}
