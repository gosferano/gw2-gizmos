namespace Gw2Gizmos.Data.Worker.Configuration;

/// <summary>
/// Supplies the GW2 API key for authenticated requests (needs the <c>account</c> + <c>tradingpost</c>
/// scopes). The source is host-specific: configuration for the headless CLI, user-entered secure
/// storage for the Herald desktop app. Implementations are read on each use, so a key supplied at
/// runtime (e.g. typed into the UI) takes effect without a restart.
/// </summary>
public interface IGw2ApiKeyProvider
{
    /// <summary>The current API key, or <c>null</c>/empty if none is configured yet.</summary>
    string? GetApiKey();
}
