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
/// no longer computed or stored here.
/// </summary>
public class ItemCraftCostUpdater
{
    private readonly Gw2GizmosDbContext _dbContext;
    private readonly ILogger<ItemCraftCostUpdater> _logger;

    public ItemCraftCostUpdater(Gw2GizmosDbContext dbContext, ILogger<ItemCraftCostUpdater> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task UpdateCraftCosts(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting craft-cost computation...");

        // Craft cost for every craftable item: walk all recipe trees once (memoized in the builder).
        var builder = new RecipeTreeBuilder(_dbContext, RecipeFinder.Configuration.Default);
        List<RecipeNode> trees = await builder.GetRecipeTrees(stoppingToken);
        if (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        // Keep the cheapest tree per output item. Untradeable ingredients price to 0 (treated as free), so a
        // recipe is included as long as its total cost is positive; we don't require every ingredient to be
        // priced. Before the first price poll all prices are 0, so every cost is 0 and the run is skipped by
        // the empty-result guard below — meaning a cached cost is only ever built against a populated price set.
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
    }
}
