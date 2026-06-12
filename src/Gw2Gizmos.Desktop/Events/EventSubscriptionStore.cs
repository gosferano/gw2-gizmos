using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.State;
using Microsoft.Extensions.DependencyInjection;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// Tracks which scheduled events the user has opted into reminders for. Persisted as a JSON id list in the
/// shared <see cref="AppState"/> table (so it survives restarts), cached in memory and written through on
/// each change. Read by the Events screen's per-row bell toggle and by <see cref="EventReminderService"/>.
/// </summary>
public sealed class EventSubscriptionStore
{
    private const string StateKey = "gw2gizmos.eventreminders";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly HashSet<string> _ids;
    private readonly object _gate = new();

    public EventSubscriptionStore(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        _ids = Load();
    }

    public bool IsSubscribed(string eventId)
    {
        lock (_gate)
        {
            return _ids.Contains(eventId);
        }
    }

    /// <summary>A snapshot of the currently-subscribed event ids (safe to enumerate off-thread).</summary>
    public IReadOnlyCollection<string> SubscribedIds
    {
        get
        {
            lock (_gate)
            {
                return _ids.ToArray();
            }
        }
    }

    public void SetSubscribed(string eventId, bool subscribed)
    {
        lock (_gate)
        {
            bool changed = subscribed ? _ids.Add(eventId) : _ids.Remove(eventId);
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
