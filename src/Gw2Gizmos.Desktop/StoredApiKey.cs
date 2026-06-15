using System;
using System.Collections.Generic;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// One stored GW2 API key plus the account/key metadata captured when it was added. Keeping the metadata
/// lets the API Keys cards render instantly (no per-load network call) and lets duplicate-account entry be
/// blocked locally. Persisted (DPAPI-encrypted) by <see cref="FileGw2ApiKeyStore"/>; only <see cref="Key"/>
/// is handed to the worker.
/// </summary>
public sealed record StoredApiKey
{
    /// <summary>The raw GW2 API key/token.</summary>
    public required string Key { get; init; }

    /// <summary>The account's stable GUID (from <c>/v2/account</c>); the dedupe key — one key per account.</summary>
    public required string AccountId { get; init; }

    /// <summary>The account display name (e.g. "Name.1234").</summary>
    public required string AccountName { get; init; }

    /// <summary>The key's own name, as set on the GW2 account page (from <c>/v2/tokeninfo</c>).</summary>
    public required string KeyName { get; init; }

    /// <summary>The permissions granted to the key (from <c>/v2/tokeninfo</c>).</summary>
    public IReadOnlyList<string> Permissions { get; init; } = Array.Empty<string>();
}
