using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Recipes;
using Gw2Gizmos.RecipeFinder;
using Gw2Gizmos.RecipeFinder.Model;
using Microsoft.EntityFrameworkCore;

namespace Gw2Gizmos.Data.Worker.Updaters;

/// <summary>
/// Rebuilds the <see cref="ItemCraftCost"/> cache the Items grid reads: the cheapest fully-priced cost to
/// craft each craftable item, computed by walking every recipe's ingredient tree with the RecipeFinder
/// engine. Replaces the table wholesale each commerce refresh (ingredient prices come from the listings, so
/// it refreshes after each commerce sync). Heavy enough to belong in the background worker; everything else
/// the grid shows — live buy/sell, profit, margin — is derived at read time from the price history, so it is
/// no longer computed or stored here. Then rebuilds the <see cref="ItemCoinCost"/> cache for valuing holdings. Both
/// are paced to about half a core (<see cref="CpuPacer"/>), as they'd otherwise hold one at full load for seconds.
/// </summary>
public class ItemCraftCostUpdater
{
    private readonly Gw2GizmosDbContext _dbContext;
    private readonly ILogger<ItemCraftCostUpdater> _logger;
    private readonly CpuPacer _pacer;

    public ItemCraftCostUpdater(
        Gw2GizmosDbContext dbContext, ILogger<ItemCraftCostUpdater> logger, TimeProvider? time = null)
    {
        _dbContext = dbContext;
        _logger = logger;
        _pacer = new CpuPacer(time ?? TimeProvider.System);
    }

    public async Task UpdateCraftCosts(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting craft-cost computation...");

        // Craft cost for every craftable item: walk all recipe trees once (memoized in the builder). The account only
        // marks completed collections in labels, not in costs; the worker doesn't know which account the desktop shows,
        // so it takes the most recently synced one (with one account, the same).
        string? accountId = await _dbContext.Accounts.AsNoTracking()
            .OrderByDescending(a => a.LastSyncedUtc)
            .Select(a => a.AccountId)
            .FirstOrDefaultAsync(stoppingToken);
        var builder = new RecipeTreeBuilder(_dbContext, RecipeFinder.Configuration.Default, accountId);
        List<RecipeNode> trees = await builder.GetRecipeTrees(stoppingToken, _pacer.PauseIfDue);
        if (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        // Keep the cheapest tree per output item. Parts that can't be bought count as 0, as in the craft tree, so a
        // precursor made from collection items keeps an estimate of what the rest costs. Before the first price poll
        // all prices are 0, so every cost is 0 and the run is skipped by the empty-result guard below — meaning a
        // cached cost is only ever built against a populated price set.
        var cheapestByItem = new Dictionary<int, RecipeNode>();
        foreach (RecipeNode tree in trees)
        {
            if (tree.CraftingCost <= 0)
            {
                continue;
            }

            if (cheapestByItem.TryGetValue(tree.ItemId, out RecipeNode? existing)
                && existing.CraftingCost <= tree.CraftingCost)
            {
                continue;
            }

            cheapestByItem[tree.ItemId] = tree;
        }

        // An empty result means ingredient prices aren't loaded yet (e.g. a commerce refresh is mid-flight),
        // not that nothing is craftable — replacing the cache here would blank every craft cost until the next
        // run, so keep the previous cache instead.
        if (cheapestByItem.Count == 0)
        {
            _logger.LogWarning(
                "Craft-cost computation produced 0 items (ingredient prices not ready); keeping the previous cache."
            );
            return;
        }

        DateTimeOffset computedAt = DateTimeOffset.UtcNow;
        var rows = cheapestByItem
            .Select(pair => new ItemCraftCost
            {
                ItemId = pair.Key,
                CraftingCost = (double)pair.Value.CraftingCost,
                ComputedAtUtc = computedAt
            })
            .ToList();

        // Replace wholesale: the cache always reflects the latest run, nothing accumulates.
        await _dbContext.ItemCraftCosts.ExecuteDeleteAsync(stoppingToken);
        await _dbContext.ItemCraftCosts.AddRangeAsync(rows, stoppingToken);
        await _dbContext.SaveChangesAsync(stoppingToken);

        _logger.LogInformation("Craft-cost computation completed: {Count} craftable items.", rows.Count);

        await UpdateCoinCosts(accountId, computedAt, stoppingToken);
    }

    /// <summary>
    /// Rebuilds the <see cref="ItemCoinCost"/> cache: for each item an account holds, has equipped or in its legendary
    /// armory, or that unlocks something it has, that the trading post doesn't buy, what it costs for coin alone
    /// (bought from a vendor or made), to value it. Parts coin can't buy (Legendary Insights, Gifts of Maguuma, …)
    /// count as nothing, as gw2efficiency counts them, so a legendary is worth what the rest of it costs; an item
    /// nothing of which costs coin gets none. Only run with prices loaded, after the craft costs.
    /// </summary>
    private async Task UpdateCoinCosts(string? accountId, DateTimeOffset computedAt, CancellationToken stoppingToken)
    {
        IQueryable<int> bought = _dbContext.PriceSnapshots
            .Where(snapshot => snapshot.Buy != null && snapshot.Buy > 0)
            .Select(snapshot => snapshot.ItemId);
        // Held, equipped, in the legendary armory, or unlocking something an account has.
        var held = (await _dbContext.AccountItemObservations.AsNoTracking()
                .Where(observation => observation.Count > 0)
                .Select(observation => observation.ItemId)
                .Distinct()
                .ToListAsync(stoppingToken))
            .Concat(await _dbContext.CharacterEquippedItems.AsNoTracking().Select(e => e.ItemId)
                .ToListAsync(stoppingToken))
            .Concat(await _dbContext.AccountLegendaryArmory.AsNoTracking().Select(l => l.ItemId)
                .ToListAsync(stoppingToken))
            .ToHashSet();
        foreach (string account in await _dbContext.Accounts.AsNoTracking().Select(a => a.AccountId)
                     .ToListAsync(stoppingToken))
        {
            held.UnionWith(AccountUnlockValuation.UnlockingItems(_dbContext, account));
        }

        HashSet<int> boughtIds = (await bought.Distinct().ToListAsync(stoppingToken)).ToHashSet();
        List<int> itemIds = held.Where(id => !boughtIds.Contains(id)).ToList();

        var builder = new RecipeTreeBuilder(_dbContext, RecipeFinder.Configuration.CoinOnly, accountId);
        var rows = new List<ItemCoinCost>();
        foreach (int itemId in itemIds)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                return;
            }

            RecipeNode tree = await builder.BuildTreeAsync(itemId, stoppingToken);
            if (tree.EffectiveCost is > 0 and var cost)
            {
                rows.Add(new ItemCoinCost { ItemId = itemId, Cost = (double)cost, ComputedAtUtc = computedAt });
            }

            _pacer.PauseIfDue(stoppingToken);
        }

        await _dbContext.ItemCoinCosts.ExecuteDeleteAsync(stoppingToken);
        await _dbContext.ItemCoinCosts.AddRangeAsync(rows, stoppingToken);
        await _dbContext.SaveChangesAsync(stoppingToken);

        _logger.LogInformation("Coin-cost computation completed: {Count} items.", rows.Count);
    }
}
