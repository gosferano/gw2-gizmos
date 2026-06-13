using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// Tracks which scheduled events the user has starred as favorites (pinned to the top of the Events list).
/// Persisted as a JSON id list in a per-user file, cached in memory and written through on each change — the
/// same pattern as <see cref="EventSubscriptionStore"/>.
/// </summary>
public sealed class EventFavoritesStore
{
    private readonly string _path;
    private readonly HashSet<string> _ids;
    private readonly object _gate = new();

    public EventFavoritesStore(AppPaths paths)
    {
        _path = paths.File("event-favorites.json");
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
        try
        {
            if (!File.Exists(_path))
            {
                return new HashSet<string>();
            }

            string[]? ids = JsonSerializer.Deserialize<string[]>(File.ReadAllText(_path));
            return ids is null ? new HashSet<string>() : new HashSet<string>(ids);
        }
        catch (Exception)
        {
            return new HashSet<string>();
        }
    }

    private void Save() => File.WriteAllText(_path, JsonSerializer.Serialize(_ids.ToArray()));
}
