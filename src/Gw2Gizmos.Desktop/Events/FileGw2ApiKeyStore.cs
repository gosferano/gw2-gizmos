using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Gw2Gizmos.Data.Worker.Configuration;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// API-key store backed by a per-user file, DPAPI-encrypted at rest. Holds one entry per account (see
/// <see cref="StoredApiKey"/>) so several accounts can sync; written by the API Keys UI and read by the
/// in-process delivery poller and the key service (which hands the raw keys to the worker). Cached in memory
/// and written through on change, so a key added in the UI takes effect immediately without a restart.
/// </summary>
public sealed class FileGw2ApiKeyStore : IGw2ApiKeyProvider
{
    private readonly string _path;
    private readonly object _gate = new();
    private List<StoredApiKey>? _cached;

    public FileGw2ApiKeyStore(AppPaths paths)
    {
        _path = paths.File("api-keys.dat");
    }

    public bool HasApiKey => Snapshot().Count > 0;

    /// <summary>All stored keys with their account/scope metadata, for the API Keys cards.</summary>
    public IReadOnlyList<StoredApiKey> GetStoredKeys() => Snapshot();

    public IReadOnlyList<string> GetApiKeys() => Snapshot().Select(k => k.Key).ToArray();

    public string? GetApiKey()
    {
        IReadOnlyList<StoredApiKey> all = Snapshot();
        return all.Count > 0 ? all[0].Key : null;
    }

    /// <summary>
    /// Adds a key. Returns false (storing nothing) when a key for the same account is already present —
    /// one key per account.
    /// </summary>
    public bool AddApiKey(StoredApiKey key)
    {
        lock (_gate)
        {
            List<StoredApiKey> all = Load();
            if (all.Any(k => string.Equals(k.AccountId, key.AccountId, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            all.Add(key);
            Save(all);
            _cached = all;
            return true;
        }
    }

    /// <summary>Removes the key for the given account id (no-op if absent).</summary>
    public void RemoveApiKey(string accountId)
    {
        lock (_gate)
        {
            List<StoredApiKey> all = Load();
            all.RemoveAll(k => string.Equals(k.AccountId, accountId, StringComparison.OrdinalIgnoreCase));
            Save(all);
            _cached = all;
        }
    }

    private IReadOnlyList<StoredApiKey> Snapshot()
    {
        lock (_gate)
        {
            _cached ??= Load();
            return _cached.ToArray();
        }
    }

    private List<StoredApiKey> Load()
    {
        try
        {
            if (!File.Exists(_path))
            {
                return new List<StoredApiKey>();
            }

            string cipher = File.ReadAllText(_path);
            if (string.IsNullOrWhiteSpace(cipher))
            {
                return new List<StoredApiKey>();
            }

            string? json = ProtectedDataHelper.Unprotect(cipher);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<StoredApiKey>();
            }

            return JsonSerializer.Deserialize<List<StoredApiKey>>(json) ?? new List<StoredApiKey>();
        }
        catch (Exception)
        {
            // Unreadable/corrupt blob (or encrypted by a different user/machine) — treat as "no keys".
            return new List<StoredApiKey>();
        }
    }

    private void Save(List<StoredApiKey> keys)
    {
        if (keys.Count == 0)
        {
            if (File.Exists(_path))
            {
                File.Delete(_path);
            }

            return;
        }

        string json = JsonSerializer.Serialize(keys);
        File.WriteAllText(_path, ProtectedDataHelper.Protect(json));
    }
}
