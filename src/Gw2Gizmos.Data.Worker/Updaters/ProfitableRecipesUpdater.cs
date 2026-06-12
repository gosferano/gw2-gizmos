using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Recipes;
using Gw2Gizmos.RecipeFinder;
using Gw2Gizmos.RecipeFinder.Model;
using Microsoft.EntityFrameworkCore;

namespace Gw2Gizmos.Data.Worker.Updaters;

/// <summary>
/// Recomputes the "profitable to craft" snapshot the desktop feed reads. Walks every recipe's full
/// ingredient tree (reusing the RecipeFinder engine), keeps the ones whose post-fee sell price beats
/// their cheapest craft cost, and replaces the <see cref="ProfitableRecipe"/> table wholesale. Heavy, so
/// it runs in the background worker on the commerce cadence rather than in the UI process.
/// </summary>
public class ProfitableRecipesUpdater
{
    /// <summary>How many of the most profitable recipes to keep in the snapshot.</summary>
    private const int TopCount = 100;

    /// <summary>Trading-post sales are charged a 15% fee (listing + exchange), so you net 85%.</summary>
    private const decimal TradingPostNetFactor = 0.85m;

    private readonly Gw2GizmosDbContext _dbContext;
    private readonly ILogger<ProfitableRecipesUpdater> _logger;

    public ProfitableRecipesUpdater(Gw2GizmosDbContext dbContext, ILogger<ProfitableRecipesUpdater> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task UpdateProfitableRecipes(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting profitable-recipes computation...");

        var builder = new RecipeTreeBuilder(_dbContext, RecipeFinder.Configuration.Default);

        List<RecipeNode> trees = await builder.GetRecipeTrees(stoppingToken);
        if (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        DateTimeOffset computedAt = DateTimeOffset.UtcNow;

        List<ProfitableRecipe> rows = trees
            // CraftCostKnown drops recipes whose cheapest path bottoms out in an unpriced base item: their
            // cost can't be trusted (an absent trading-post price would otherwise be treated as free).
            .Where(node => node.CraftCostKnown && node.CraftingCost > 0)
            .Select(node => (Node: node, Profit: (node.SellPrice * TradingPostNetFactor) - node.CraftingCost))
            .Where(x => x.Profit > 0)
            .OrderByDescending(x => x.Profit)
            .Take(TopCount)
            .Select(x => new ProfitableRecipe
            {
                OutputItemId = x.Node.ItemId,
                OutputItemName = x.Node.ItemName,
                // Computed precisely in decimal, then stored as double (the entity's SQL-orderable type).
                CraftingCost = (double)x.Node.CraftingCost,
                SellPrice = x.Node.SellPrice,
                BuyPrice = x.Node.BuyPrice,
                Profit = (double)x.Profit,
                MarginPercent = x.Node.CraftingCost > 0 ? (double)(x.Profit / x.Node.CraftingCost * 100m) : 0d,
                ComputedAtUtc = computedAt,
                TreeJson = RecipeTreeSerializer.Serialize(x.Node)
            })
            .ToList();

        // Replace the snapshot wholesale: the feed always reflects the latest run, nothing accumulates.
        await _dbContext.ProfitableRecipes.ExecuteDeleteAsync(stoppingToken);
        await _dbContext.ProfitableRecipes.AddRangeAsync(rows, stoppingToken);
        await _dbContext.SaveChangesAsync(stoppingToken);

        _logger.LogInformation("Profitable-recipes computation completed: {Count} profitable recipes.", rows.Count);
    }
}
