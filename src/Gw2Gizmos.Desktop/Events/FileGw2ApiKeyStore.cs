using System;
using System.IO;
using Gw2Gizmos.Data.Worker.Configuration;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// API-key store backed by a per-user file, DPAPI-encrypted at rest. Replaces the shared-DB store now that
/// the desktop never writes the ingestion database. Written by the Settings UI and read by the in-process
/// delivery poller; cached in memory and written through on change, so a key entered in the UI takes effect
/// immediately without a restart. When account-data sync moves to the worker, the desktop will pass the key
/// to that process at spawn (env), not via shared storage.
/// </summary>
public sealed class FileGw2ApiKeyStore : IGw2ApiKeyProvider
{
    private readonly string _path;
    private readonly object _gate = new();
    private string? _cached;
    private bool _loaded;

    public FileGw2ApiKeyStore(AppPaths paths)
    {
        _path = paths.File("api-key.dat");
    }

    public bool HasApiKey => !string.IsNullOrWhiteSpace(GetApiKey());

    public string? GetApiKey()
    {
        lock (_gate)
        {
            if (!_loaded)
            {
                _cached = Load();
                _loaded = true;
            }

            return _cached;
        }
    }

    /// <summary>Stores (or clears, when blank) the API key — encrypted — in the per-user file.</summary>
    public void SetApiKey(string? apiKey)
    {
        string? normalized = string.IsNullOrWhiteSpace(apiKey) ? null : apiKey.Trim();

        lock (_gate)
        {
            if (normalized is null)
            {
                if (File.Exists(_path))
                {
                    File.Delete(_path);
                }
            }
            else
            {
                File.WriteAllText(_path, ProtectedDataHelper.Protect(normalized));
            }

            _cached = normalized;
            _loaded = true;
        }
    }

    private string? Load()
    {
        try
        {
            if (!File.Exists(_path))
            {
                return null;
            }

            string cipher = File.ReadAllText(_path);
            return string.IsNullOrWhiteSpace(cipher) ? null : ProtectedDataHelper.Unprotect(cipher);
        }
        catch (Exception)
        {
            // Unreadable/corrupt blob (or encrypted by a different user/machine) — treat as "no key".
            return null;
        }
    }
}
