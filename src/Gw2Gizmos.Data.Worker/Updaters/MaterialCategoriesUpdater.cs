using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Materials;
using Gw2Gizmos.Gw2Api.Client;
using Microsoft.EntityFrameworkCore;
using Api = Gw2Gizmos.Gw2Api.Contract.V2.Materials;

namespace Gw2Gizmos.Data.Worker.Updaters;

/// <summary>
/// Full-refresh ingester for material-storage category master data from <c>/v2/materials</c> (public; no key).
/// Cheap — each category is just an id, a name, and an ordered item-id list — so it runs on the daily items
/// cadence. Drives the in-game-style grouped material grid on the Account screen.
/// </summary>
public class MaterialCategoriesUpdater
{
    private readonly Gw2GizmosDbContext _dbContext;
    private readonly Gw2ApiClient _apiClient;
    private readonly ILogger<MaterialCategoriesUpdater> _logger;

    public MaterialCategoriesUpdater(
        Gw2GizmosDbContext dbContext,
        IGw2ApiClientFactory apiClientFactory,
        ILogger<MaterialCategoriesUpdater> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
        _apiClient = apiClientFactory.Create(Locale.English);
    }

    public async Task UpdateMaterialCategories(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting material categories update...");

        Api.MaterialCategory[]? categories = await _apiClient.V2.Materials.GetAll(stoppingToken);
        if (categories is null || categories.Length == 0)
        {
            _logger.LogWarning("Materials API returned no data; skipping material categories update.");
            return;
        }

        Dictionary<int, MaterialCategory> existing = await _dbContext.MaterialCategories.ToDictionaryAsync(c => c.Id, stoppingToken);
        foreach (Api.MaterialCategory category in categories)
        {
            if (existing.TryGetValue(category.Id, out MaterialCategory? row))
            {
                row.Name = category.Name;
                row.Order = category.Order;
            }
            else
            {
                _dbContext.MaterialCategories.Add(new MaterialCategory { Id = category.Id, Name = category.Name, Order = category.Order });
            }
        }

        // Membership is master data; replace it wholesale (a few hundred rows). ExecuteDelete runs immediately,
        // so re-inserting the same composite keys in the SaveChanges below can't collide.
        await _dbContext.MaterialCategoryItems.ExecuteDeleteAsync(stoppingToken);
        foreach (Api.MaterialCategory category in categories)
        {
            for (int position = 0; position < category.Items.Length; position++)
            {
                _dbContext.MaterialCategoryItems.Add(
                    new MaterialCategoryItem { CategoryId = category.Id, ItemId = category.Items[position], Position = position }
                );
            }
        }

        await _dbContext.SaveChangesAsync(stoppingToken);
        _logger.LogInformation("Material categories update completed: {Count} categories.", categories.Length);
    }
}
