using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Gw2Gizmos.Data.Worker.Features;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// Persists which worker features the user has enabled (a per-user JSON file). The desktop's Settings page binds
/// to it, and the <see cref="WorkerConfigHost"/> pushes the enabled set to the worker, which gates its sync work
/// on it. Unset features fall back to the desktop default (on, except <see cref="OffByDefault"/>). Cached in
/// memory and written through on change; raises <see cref="Changed"/> so the API Keys page recomputes its
/// permission highlighting.
/// </summary>
public sealed class FeatureSettingsStore
{
    // The desktop's default for features the user hasn't touched: everything on, except ones costly enough to be
    // opt-in here (price history grows the DB quickly). This default is a desktop policy, not part of the shared
    // feature definition — a standalone worker takes its defaults from appsettings.json (Worker:Features:*).
    private static readonly HashSet<string> OffByDefault = new(StringComparer.OrdinalIgnoreCase)
    {
        WorkerFeatures.PricesSync.Key,
    };

    private readonly string _path;
    private readonly object _gate = new();
    private readonly Dictionary<string, bool> _states;

    public FeatureSettingsStore(AppPaths paths)
    {
        _path = paths.File("feature-settings.json");
        _states = Load();
    }

    /// <summary>Raised when any feature toggles, so views recompute (e.g. the API Keys permission chips).</summary>
    public event Action? Changed;

    /// <summary>The user's choice, or the desktop default (on for everything except <see cref="OffByDefault"/>).</summary>
    public bool IsEnabled(string featureKey)
    {
        lock (_gate)
        {
            if (_states.TryGetValue(featureKey, out bool enabled))
            {
                return enabled;
            }
        }

        return !OffByDefault.Contains(featureKey);
    }

    public void SetEnabled(string featureKey, bool enabled)
    {
        lock (_gate)
        {
            if (_states.TryGetValue(featureKey, out bool current) && current == enabled)
            {
                return;
            }

            _states[featureKey] = enabled;
            Save(_states);
        }

        Changed?.Invoke();
    }

    /// <summary>The feature keys currently enabled — the set handed to the worker over the pipe.</summary>
    public IReadOnlyList<string> EnabledKeys() =>
        WorkerFeatures.All.Where(feature => IsEnabled(feature.Key)).Select(feature => feature.Key).ToArray();

    private Dictionary<string, bool> Load()
    {
        try
        {
            if (!File.Exists(_path))
            {
                return new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            }

            Dictionary<string, bool>? states =
                JsonSerializer.Deserialize<Dictionary<string, bool>>(File.ReadAllText(_path));
            return states is null
                ? new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, bool>(states, StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception)
        {
            return new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private void Save(Dictionary<string, bool> states) =>
        File.WriteAllText(_path, JsonSerializer.Serialize(states));
}
