using System.Collections.Generic;

namespace Gw2Gizmos.Data.Worker.Configuration;

/// <summary>
/// Supplies the GW2 API key(s) for authenticated requests (needs the <c>account</c> + <c>tradingpost</c>
/// scopes). The source is host-specific: configuration for the headless CLI, user-entered secure
/// storage for the Gw2Gizmos desktop app. Implementations are read on each use, so keys supplied at
/// runtime (e.g. added in the UI) take effect without a restart. Multiple keys are supported — one per
/// account — so the worker can sync several accounts.
/// </summary>
public interface IGw2ApiKeyProvider
{
    /// <summary>The first configured API key, or <c>null</c>/empty if none. Convenience for single-key consumers.</summary>
    string? GetApiKey();

    /// <summary>All configured API keys (one per account); empty when none.</summary>
    IReadOnlyList<string> GetApiKeys();
}
