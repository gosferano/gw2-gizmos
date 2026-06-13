using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.State;
using Microsoft.Extensions.DependencyInjection;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// Tracks which scheduled events the user has starred as favorites (pinned to the top of the Events list).
/// Persisted as a JSON id list in the shared <see cref="AppState"/> table, cached in memory and written
/// through on each change — the same pattern as <see cref="EventSubscriptionStore"/>.
/// </summary>
public sealed class EventFavoritesStore
{
    private const string StateKey = "gw2gizmos.eventfavorites";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly HashSet<string> _ids;
    private readonly object _gate = new();

    public EventFavoritesStore(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        _ids = Load();
    }

    public bool IsFavorite(string eventId)
    {
        lock (_gate)
        {
            return _ids.Contains(eventId);
        }
    }

    public void SetFavorite(string eventId, bool favorite)
    {
        lock (_gate)
        {
            bool changed = favorite ? _ids.Add(eventId) : _ids.Remove(eventId);
            if (!changed)
            {
                return;
            }

            Save();
        }
    }

    private HashSet<string> Load()
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();
        AppState? row = dbContext.AppState.FirstOrDefault(s => s.Key == StateKey);

        if (row is null || string.IsNullOrWhiteSpace(row.Value))
        {
            return new HashSet<string>();
        }

        string[]? ids = JsonSerializer.Deserialize<string[]>(row.Value);
        return ids is null ? new HashSet<string>() : new HashSet<string>(ids);
    }

    private void Save()
    {
        string json = JsonSerializer.Serialize(_ids.ToArray());

        using IServiceScope scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();
        AppState? row = dbContext.AppState.FirstOrDefault(s => s.Key == StateKey);

        if (row is null)
        {
            dbContext.AppState.Add(new AppState { Key = StateKey, Value = json });
        }
        else
        {
            row.Value = json;
        }

        dbContext.SaveChanges();
    }
}
