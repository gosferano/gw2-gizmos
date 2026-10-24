using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Gw2Gizmos.Data.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>Where an item's <see cref="ItemPrice.Value"/> comes from.</summary>
public enum ItemValueSource
{
    /// <summary>No value: not bought on the trading post, not to be had for coin, and not sold to vendors.</summary>
    None,

    /// <summary>The highest buy order, after the trading post's fees.</summary>
    InstantSell,

    /// <summary>
    /// What it costs for coin alone (made, or bought from a vendor), as the trading post doesn't buy it.
    /// </summary>
    CoinCost,

    /// <summary>What a vendor pays for it.</summary>
    Vendor,
}

/// <summary>
/// An item's prices for its hover text: the latest highest buy order and lowest listing (null when the trading post
/// has none), and what gizmos counts it as worth (<see cref="AccountHoldingsReconstructor.ItemValues"/>, with coin
/// costs, as the account value does).
/// </summary>
public sealed record ItemPrice(string Name, int? Buy, int? Sell, long Value, ItemValueSource Source);

/// <summary>
/// Looks up an item's prices when its hover text opens, one item at a time, remembering each for a minute so hovering
/// back and forth doesn't query again.
/// </summary>
public sealed class ItemPrices
{
    private static readonly TimeSpan KeepFor = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConcurrentDictionary<int, (DateTimeOffset At, ItemPrice? Price)> _recent = new();

    public ItemPrices(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    /// <summary>The item's prices, or null when the catalog doesn't have it (or the database can't be read).</summary>
    public async Task<ItemPrice?> GetAsync(int itemId)
    {
        if (_recent.TryGetValue(itemId, out var recent) && DateTimeOffset.UtcNow - recent.At < KeepFor)
        {
            return recent.Price;
        }

        ItemPrice? price = await Task.Run(() => Load(itemId));
        _recent[itemId] = (DateTimeOffset.UtcNow, price);
        return price;
    }

    private ItemPrice? Load(int itemId)
    {
        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();
            var item = db.Items.AsNoTracking()
                .Where(i => i.Id == itemId)
                .Select(i => new { i.Name, i.VendorValue, NoSell = i.Flags.Any(f => f.Value == "NoSell") })
                .FirstOrDefault();
            if (item is null)
            {
                return null;
            }

            var snapshot = db.PriceSnapshots.AsNoTracking()
                .Where(s => s.ItemId == itemId)
                .OrderByDescending(s => s.Id)
                .Select(s => new { s.Buy, s.Sell })
                .FirstOrDefault();
            int? buy = snapshot?.Buy is > 0 ? snapshot.Buy : null;
            int? sell = snapshot?.Sell is > 0 ? snapshot.Sell : null;
            long value = AccountHoldingsReconstructor.ItemValues(db, [itemId], coinCosts: true)
                .GetValueOrDefault(itemId);
            bool coinCost = buy is null && db.ItemCoinCosts.AsNoTracking().Any(c => c.ItemId == itemId);
            ItemValueSource source = value <= 0 ? ItemValueSource.None
                : buy is not null ? ItemValueSource.InstantSell
                : coinCost ? ItemValueSource.CoinCost
                : ItemValueSource.Vendor;
            return new ItemPrice(item.Name, buy, sell, value, source);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
