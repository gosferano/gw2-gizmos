using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gw2Gizmos.Data.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// Every item's rarity (e.g. "Exotic"), by id, for colouring item names and icons the way the game does. Loaded once
/// from the item catalog in the background (~74k small entries); controls rendered before it's ready wait for
/// <see cref="Ready"/>. Items the catalog didn't have yet read as unknown (uncoloured) until the next app start.
/// </summary>
public sealed class ItemRarities
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Lazy<Task<Dictionary<int, string>>> _rarities;

    public ItemRarities(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        _rarities = new Lazy<Task<Dictionary<int, string>>>(() => Task.Run(Load));
    }

    /// <summary>Starts loading the rarities in the background, if not already started (e.g. at app startup, so they're
    /// ready before the first page shows items).</summary>
    public void StartLoading() => _ = _rarities.Value;

    /// <summary>Completes once the rarities are loaded (starting the load if it hasn't started).</summary>
    public Task Ready => _rarities.Value;

    /// <summary>The item's rarity, or null when unknown or not loaded yet (await <see cref="Ready"/> first).</summary>
    public string? Of(int itemId) =>
        _rarities.Value is { IsCompletedSuccessfully: true } loaded ? loaded.Result.GetValueOrDefault(itemId) : null;

    private Dictionary<int, string> Load()
    {
        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();
            return db.Items.AsNoTracking().Select(i => new { i.Id, i.Rarity }).ToDictionary(i => i.Id, i => i.Rarity);
        }
        catch (Exception)
        {
            // Database not ready yet (fresh install): items stay uncoloured.
            return new Dictionary<int, string>();
        }
    }
}
