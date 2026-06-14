using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Gw2Gizmos.Data.Worker.Features;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// Persists the user's per-sync interval overrides in a per-user JSON file (each value a <see cref="TimeSpan"/>
/// string such as <c>"00:05:00"</c>). The Advanced settings page binds to it; the worker-config pipe pushes the
/// full interval map to the worker, which retimes its loops. Cached in memory, written through on change; raises
/// <see cref="Changed"/> for dependent views. Unset syncs fall back to the <see cref="WorkerSyncs"/> catalog
/// defaults.
/// </summary>
public sealed class IntervalSettingsStore
{
    private readonly string _path;
    private readonly object _gate = new();
    private readonly Dictionary<string, TimeSpan> _intervals;

    public IntervalSettingsStore(AppPaths paths)
    {
        _path = paths.File("interval-settings.json");
        _intervals = Load();
    }

    /// <summary>Raised when any interval changes, so the worker pushes the new map on its next fetch.</summary>
    public event Action? Changed;

    /// <summary>The user's chosen cadence for a sync, or the catalog default.</summary>
    public TimeSpan GetInterval(string syncKey)
    {
        lock (_gate)
        {
            if (_intervals.TryGetValue(syncKey, out TimeSpan interval) && interval > TimeSpan.Zero)
            {
                return interval;
            }
        }

        return WorkerSyncs.DefaultInterval(syncKey);
    }

    public void SetInterval(string syncKey, TimeSpan interval)
    {
        lock (_gate)
        {
            if (_intervals.TryGetValue(syncKey, out TimeSpan current) && current == interval)
            {
                return;
            }

            _intervals[syncKey] = interval;
            Save(_intervals);
        }

        Changed?.Invoke();
    }

    /// <summary>The full interval map (every catalog sync at its effective cadence) handed to the worker.</summary>
    public IReadOnlyDictionary<string, TimeSpan> AllIntervals() =>
        WorkerSyncs.All.ToDictionary(sync => sync.Key, sync => GetInterval(sync.Key));

    private Dictionary<string, TimeSpan> Load()
    {
        try
        {
            if (!File.Exists(_path))
            {
                return new Dictionary<string, TimeSpan>(StringComparer.OrdinalIgnoreCase);
            }

            Dictionary<string, TimeSpan>? stored =
                JsonSerializer.Deserialize<Dictionary<string, TimeSpan>>(File.ReadAllText(_path));
            return stored is null
                ? new Dictionary<string, TimeSpan>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, TimeSpan>(stored, StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception)
        {
            return new Dictionary<string, TimeSpan>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private void Save(Dictionary<string, TimeSpan> intervals) =>
        File.WriteAllText(_path, JsonSerializer.Serialize(intervals));
}
