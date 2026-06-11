using System;
using System.Linq;
using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.State;
using Gw2Gizmos.Data.Worker.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gw2Gizmos.Herald;

/// <summary>
/// Herald's API-key source: holds the user-entered GW2 API key, persisted DPAPI-encrypted in the
/// <see cref="AppState"/> table. Implements <see cref="IGw2ApiKeyProvider"/> so the engine's delivery
/// poller reads it; the settings UI calls <see cref="SetApiKey"/> to update it at runtime.
/// </summary>
public sealed class HeraldApiKeyStore : IGw2ApiKeyProvider
{
    private const string StateKey = "herald.apikey";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly object _gate = new();
    private string? _cachedKey;
    private bool _loaded;

    public HeraldApiKeyStore(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public bool HasApiKey => !string.IsNullOrWhiteSpace(GetApiKey());

    public string? GetApiKey()
    {
        lock (_gate)
        {
            if (!_loaded)
            {
                _cachedKey = Load();
                _loaded = true;
            }

            return _cachedKey;
        }
    }

    /// <summary>Stores (or clears, when blank) the API key — encrypted — and updates the cache.</summary>
    public void SetApiKey(string? apiKey)
    {
        string? normalized = string.IsNullOrWhiteSpace(apiKey) ? null : apiKey.Trim();

        lock (_gate)
        {
            Save(normalized);
            _cachedKey = normalized;
            _loaded = true;
        }
    }

    private string? Load()
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();
        AppState? row = dbContext.AppState.FirstOrDefault(s => s.Key == StateKey);
        return row is null ? null : ProtectedDataHelper.Unprotect(row.Value);
    }

    private void Save(string? apiKey)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();
        AppState? row = dbContext.AppState.FirstOrDefault(s => s.Key == StateKey);

        if (apiKey is null)
        {
            if (row is not null)
            {
                dbContext.AppState.Remove(row);
            }
        }
        else
        {
            string encrypted = ProtectedDataHelper.Protect(apiKey);
            if (row is null)
            {
                dbContext.AppState.Add(new AppState { Key = StateKey, Value = encrypted });
            }
            else
            {
                row.Value = encrypted;
            }
        }

        dbContext.SaveChanges();
    }
}
