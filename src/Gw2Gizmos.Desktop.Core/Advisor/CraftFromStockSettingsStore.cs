using System;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// Craft from stock's filters: the minimum profit per craft, and whether only what the account can craft is shown.
/// The Advisor's summary counts with the same. Persisted in a per-user file, cached in memory and written through on
/// change.
/// </summary>
public sealed class CraftFromStockSettingsStore
{
    private readonly string _path;
    private readonly object _gate = new();
    private Settings _settings;

    public CraftFromStockSettingsStore(AppPaths paths)
    {
        _path = paths.File("craft-from-stock-settings.json");
        _settings = Load();
    }

    /// <summary>The minimum profit per craft, one of <see cref="CraftFromStockViewModel.Minimums"/>.</summary>
    public StockCraftMinimum Minimum
    {
        get
        {
            lock (_gate)
            {
                return MinimumOf(_settings.MinimumCopper);
            }
        }
        set => Update(_settings with { MinimumCopper = MinimumOf(value.Copper).Copper });
    }

    /// <summary>Show only recipes the account has learned, at a level one of its characters has.</summary>
    public bool OnlyCraftable
    {
        get
        {
            lock (_gate)
            {
                return _settings.OnlyCraftable;
            }
        }
        set => Update(_settings with { OnlyCraftable = value });
    }

    private static Settings Default => new(CraftFromStockViewModel.DefaultMinimum.Copper, true);

    private sealed record Settings(long MinimumCopper, bool OnlyCraftable);

    // An amount that isn't offered (an edited file) falls back to the default.
    private static StockCraftMinimum MinimumOf(long copper) =>
        CraftFromStockViewModel.Minimums.FirstOrDefault(m => m.Copper == copper)
        ?? CraftFromStockViewModel.DefaultMinimum;

    private void Update(Settings updated)
    {
        lock (_gate)
        {
            if (updated == _settings)
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
            return File.Exists(_path)
                ? JsonSerializer.Deserialize<Settings>(File.ReadAllText(_path)) ?? Default
                : Default;
        }
        catch (Exception)
        {
            return Default;
        }
    }
}
