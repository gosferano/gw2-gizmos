using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Commerce;
using Gw2Gizmos.RecipeFinder;
using Gw2Gizmos.RecipeFinder.Model;
using Microsoft.EntityFrameworkCore;

namespace Gw2Gizmos.Data.Worker.Updaters;

/// <summary>
/// Rebuilds the <see cref="MarketItem"/> snapshot the desktop Market grid reads: one flat row per
/// tradeable item with its best buy/sell prices, plus craft cost / profit for craftable items (computed
/// by walking every recipe's ingredient tree with the RecipeFinder engine). Replaces the table wholesale
/// each commerce refresh. Heavy enough to belong in the background worker rather than the UI process.
/// </summary>
public class MarketUpdater
{
    /// <summary>Trading-post sales are charged a 15% fee (listing + exchange), so you net 85%.</summary>
    private const double TradingPostNetFactor = 0.85;

    private readonly Gw2GizmosDbContext _dbContext;
    private readonly ILogger<MarketUpdater> _logger;

    public MarketUpdater(Gw2GizmosDbContext dbContext, ILogger<MarketUpdater> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task UpdateMarket(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting market snapshot computation...");

        // Craft cost for every craftable item: walk all recipe trees once (memoized in the builder).
        var builder = new RecipeTreeBuilder(_dbContext, RecipeFinder.Configuration.Default);
        List<RecipeNode> trees = await builder.GetRecipeTrees(stoppingToken);
        if (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        // An item is craftable if any recipe outputs it; keep the cheapest fully-priced tree per item.
        var craftableIds = new HashSet<int>(trees.Select(tree => tree.ItemId));
        var craftByItem = new Dictionary<int, RecipeNode>();
        foreach (RecipeNode tree in trees)
        {
            if (!tree.CraftCostKnown || tree.CraftingCost <= 0)
            {
                continue;
            }

            if (craftByItem.TryGetValue(tree.ItemId, out RecipeNode? existing)
                && existing.CraftingCost <= tree.CraftingCost)
            {
                continue;
            }

            craftByItem[tree.ItemId] = tree;
        }

        // Best price + total quantity per item in one grouped query per side, rather than point lookups.
        // Demand/Supply are the totals across every price tier (the in-game figures).
        Dictionary<int, BookSide> buyByItem = await _dbContext
            .BuyListings.GroupBy(listing => listing.CommerceItemListingId)
            .Select(group => new
            {
                ItemId = group.Key,
                Price = group.Max(listing => listing.UnitPrice),
                Quantity = group.Sum(listing => listing.Quantity)
            })
            .ToDictionaryAsync(x => x.ItemId, x => new BookSide(x.Price, x.Quantity), stoppingToken);

        Dictionary<int, BookSide> sellByItem = await _dbContext
            .SellListings.GroupBy(listing => listing.CommerceItemListingId)
            .Select(group => new
            {
                ItemId = group.Key,
                Price = group.Min(listing => listing.UnitPrice),
                Quantity = group.Sum(listing => listing.Quantity)
            })
            .ToDictionaryAsync(x => x.ItemId, x => new BookSide(x.Price, x.Quantity), stoppingToken);

        Dictionary<int, string> names = await _dbContext
            .Items.Select(item => new { item.Id, item.Name })
            .ToDictionaryAsync(item => item.Id, item => item.Name, stoppingToken);

        // The tradeable universe: anything with a buy order or a sell listing.
        var tradeableIds = new HashSet<int>(buyByItem.Keys);
        tradeableIds.UnionWith(sellByItem.Keys);

        DateTimeOffset computedAt = DateTimeOffset.UtcNow;
        var rows = new List<MarketItem>(tradeableIds.Count);
        foreach (int itemId in tradeableIds)
        {
            BookSide buy = buyByItem.GetValueOrDefault(itemId);
            BookSide sell = sellByItem.GetValueOrDefault(itemId);

            double? craftingCost = null;
            double? profit = null;
            double? marginPercent = null;
            if (craftByItem.TryGetValue(itemId, out RecipeNode? node))
            {
                craftingCost = (double)node.CraftingCost;
                // Profit from crafting then dumping into buy orders (the realizable-now sale), after fee.
                profit = (buy.Price * TradingPostNetFactor) - craftingCost;
                marginPercent = craftingCost > 0 ? profit / craftingCost * 100d : 0d;
            }

            rows.Add(new MarketItem
            {
                ItemId = itemId,
                Name = names.GetValueOrDefault(itemId) ?? $"Unknown ({itemId})",
                Buy = buy.Price,
                Demand = buy.Quantity,
                Sell = sell.Price,
                Supply = sell.Quantity,
                IsCraftable = craftableIds.Contains(itemId),
                CraftingCost = craftingCost,
                Profit = profit,
                MarginPercent = marginPercent,
                ComputedAtUtc = computedAt
            });
        }

        // Replace wholesale: the grid always reflects the latest run, nothing accumulates. (Price history
        // is recorded separately by the higher-frequency PriceSnapshotUpdater, not from here.)
        await _dbContext.MarketItems.ExecuteDeleteAsync(stoppingToken);
        await _dbContext.MarketItems.AddRangeAsync(rows, stoppingToken);
        await _dbContext.SaveChangesAsync(stoppingToken);

        _logger.LogInformation("Market snapshot computation completed: {Count} tradeable items.", rows.Count);
    }

    /// <summary>Best price and total quantity for one side of an item's order book; default is empty (0, 0).</summary>
    private readonly record struct BookSide(int Price, int Quantity);
}
