using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// The user's price alerts, persisted as JSON in a per-user file, cached in memory and written through on each
/// change. Edited from the Items and Price alerts pages; checked by the price alert service, which records
/// each alert's last price and trigger state here too.
/// </summary>
public sealed class PriceAlertStore
{
    private static readonly JsonSerializerOptions Json = new() { Converters = { new JsonStringEnumConverter() } };

    private readonly string _path;
    private readonly object _gate = new();
    private List<PriceAlert> _alerts;

    public PriceAlertStore(AppPaths paths)
    {
        _path = paths.File("price-alerts.json");
        _alerts = Load();
    }

    /// <summary>Raised after any change (on the thread that made it).</summary>
    public event Action? Changed;

    /// <summary>A snapshot of the alerts, in the order they were added.</summary>
    public IReadOnlyList<PriceAlert> All
    {
        get
        {
            lock (_gate)
            {
                return _alerts.ToArray();
            }
        }
    }

    /// <summary>Adds <paramref name="alert"/>, or replaces the alert with the same id (an edit).</summary>
    public void Save(PriceAlert alert) => Mutate(alerts =>
    {
        int index = alerts.FindIndex(a => a.Id == alert.Id);
        if (index >= 0)
        {
            alerts[index] = alert;
        }
        else
        {
            alerts.Add(alert);
        }
    });

    /// <summary>Applies updated alerts (e.g. a price check's results), ignoring any removed in the meantime.</summary>
    public void Update(IEnumerable<PriceAlert> updated) => Mutate(alerts =>
    {
        foreach (PriceAlert alert in updated)
        {
            int index = alerts.FindIndex(a => a.Id == alert.Id);
            if (index >= 0)
            {
                alerts[index] = alert;
            }
        }
    });

    public void Remove(Guid id) => Mutate(alerts => alerts.RemoveAll(a => a.Id == id));

    private void Mutate(Action<List<PriceAlert>> change)
    {
        lock (_gate)
        {
            change(_alerts);
            File.WriteAllText(_path, JsonSerializer.Serialize(_alerts, Json));
        }

        Changed?.Invoke();
    }

    private List<PriceAlert> Load()
    {
        try
        {
            return File.Exists(_path)
                ? JsonSerializer.Deserialize<List<PriceAlert>>(File.ReadAllText(_path), Json) ?? new List<PriceAlert>()
                : new List<PriceAlert>();
        }
        catch (Exception)
        {
            return new List<PriceAlert>();
        }
    }
}
