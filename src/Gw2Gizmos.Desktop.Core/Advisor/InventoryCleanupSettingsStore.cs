using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// Inventory cleanup's settings: how much material storage holds per item (null to guess it from what's stored), and
/// the items the user keeps, whose suggestions are hidden. Persisted in a per-user file, cached in memory and written
/// through on change.
/// </summary>
public sealed class InventoryCleanupSettingsStore
{
    /// <summary>The capacities to pick from: material storage grows by 250 per upgrade.</summary>
    public static readonly int[] CapacityChoices = Enumerable.Range(1, 12).Select(n => n * 250).ToArray();

    private readonly string _path;
    private readonly object _gate = new();
    private Settings _settings;

    public InventoryCleanupSettingsStore(AppPaths paths)
    {
        _path = paths.File("inventory-cleanup-settings.json");
        _settings = Load();
    }

    /// <summary>
    /// How much material storage holds per item, one of <see cref="CapacityChoices"/>; null to guess.
    /// </summary>
    public int? StorageCapacity
    {
        get
        {
            lock (_gate)
            {
                return _settings.StorageCapacity;
            }
        }
        set => Update(_settings with { StorageCapacity = Valid(value) });
    }

    /// <summary>The items whose suggestions are hidden.</summary>
    public IReadOnlySet<int> Kept
    {
        get
        {
            lock (_gate)
            {
                return _settings.Kept.ToHashSet();
            }
        }
    }

    public void Keep(int itemId, bool keep)
    {
        lock (_gate)
        {
            HashSet<int> kept = _settings.Kept.ToHashSet();
            if (keep ? kept.Add(itemId) : kept.Remove(itemId))
            {
                Update(_settings with { Kept = kept.Order().ToArray() });
            }
        }
    }

    private sealed record Settings(int? StorageCapacity, int[] Kept);

    private static int? Valid(int? capacity) => capacity is { } c && CapacityChoices.Contains(c) ? c : null;

    private void Update(Settings updated)
    {
        lock (_gate)
        {
            if (updated.StorageCapacity == _settings.StorageCapacity && updated.Kept.SequenceEqual(_settings.Kept))
            {
                return;
            }

            _settings = updated;
            File.WriteAllText(_path, JsonSerializer.Serialize(updated));
        }
    }

    private Settings Load()
    {
        try
        {
            if (File.Exists(_path)
                && JsonSerializer.Deserialize<Settings>(File.ReadAllText(_path)) is { } settings)
            {
                return new Settings(Valid(settings.StorageCapacity), settings.Kept ?? []);
            }
        }
        catch (Exception)
        {
            // Unreadable: start over.
        }

        return new Settings(null, []);
    }
}
