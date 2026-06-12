using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.State;

namespace Gw2Gizmos.Data.Worker.Configuration;

/// <summary>
/// API-key store backed by the shared <see cref="AppState"/> table, DPAPI-encrypted at rest. Written
/// by Gw2Gizmos's settings UI and read by every engine consumer — Gw2Gizmos's in-process delivery poller
/// and the background worker process alike — since they share the same database. Reads hit the DB on
/// each call, so a key changed in one process is picked up by the other without a restart.
/// </summary>
public sealed class AppStateApiKeyStore : IGw2ApiKeyProvider
{
    private const string StateKey = "gw2gizmos.apikey";

    private readonly IServiceScopeFactory _scopeFactory;

    public AppStateApiKeyStore(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public bool HasApiKey => !string.IsNullOrWhiteSpace(GetApiKey());

    public string? GetApiKey()
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();
        AppState? row = dbContext.AppState.FirstOrDefault(s => s.Key == StateKey);
        return row is null ? null : ProtectedDataHelper.Unprotect(row.Value);
    }

    /// <summary>Stores (or clears, when blank) the API key — encrypted — in the shared database.</summary>
    public void SetApiKey(string? apiKey)
    {
        string? normalized = string.IsNullOrWhiteSpace(apiKey) ? null : apiKey.Trim();

        using IServiceScope scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();
        AppState? row = dbContext.AppState.FirstOrDefault(s => s.Key == StateKey);

        if (normalized is null)
        {
            if (row is not null)
            {
                dbContext.AppState.Remove(row);
            }
        }
        else
        {
            string encrypted = ProtectedDataHelper.Protect(normalized);
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
