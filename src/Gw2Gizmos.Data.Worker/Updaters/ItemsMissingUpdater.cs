using System.Threading.Channels;
using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Items;
using Gw2Gizmos.Gw2Api.Client;
using Microsoft.EntityFrameworkCore;

namespace Gw2Gizmos.Data.Worker.Updaters;

/// <summary>
/// Single background consumer of the <see cref="ItemMissingDto"/> channel — the only path that
/// upserts items. Batches queued ids, fetches them from the API, maps them with
/// <see cref="ItemEntityMapper"/>, and upserts them. Ids come from the scheduled refresh
/// (<see cref="ItemsUpdater.UpdateItems"/>) when it finds catalogue ids absent from the items table.
/// As the sole writer, it never collides on the Items key.
/// </summary>
public class ItemsMissingUpdater : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Channel<ItemMissingDto> _itemsMissingChannel;
    private readonly Gw2ApiClient _apiClient;
    private readonly ILogger<ItemsMissingUpdater> _logger;

    private const int BatchSize = 50;

    public ItemsMissingUpdater(
        IServiceProvider serviceProvider,
        Channel<ItemMissingDto> itemsMissingChannel,
        IGw2ApiClientFactory apiClientFactory,
        ILogger<ItemsMissingUpdater> logger
    )
    {
        _serviceProvider = serviceProvider;
        _itemsMissingChannel = itemsMissingChannel;
        _apiClient = apiClientFactory.Create(Locale.English);
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Items missing updater started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                while (await _itemsMissingChannel.Reader.WaitToReadAsync(stoppingToken))
                {
                    var itemIds = new List<int>();

                    while (_itemsMissingChannel.Reader.TryRead(out ItemMissingDto? itemMissing))
                    {
                        itemIds.Add(itemMissing.ItemId);

                        if (itemIds.Count == BatchSize)
                        {
                            break;
                        }
                    }

                    if (itemIds.Count != 0)
                    {
                        await ProcessBatchAsync(itemIds.ToArray(), stoppingToken);
                    }
                }
            }
            catch (TaskCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in items missing updater.");
            }
        }

        _logger.LogInformation("Items missing updater stopped.");
    }

    private async Task ProcessBatchAsync(int[] itemIds, CancellationToken stoppingToken)
    {
        try
        {
            using IServiceScope scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();
            _logger.LogInformation("Processing missing items with IDs: {ItemIds}", string.Join(", ", itemIds));
            await UpdateItemsWithIds(dbContext, itemIds, stoppingToken);
        }
        catch (TaskCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing missing items with IDs: {ItemIds}", string.Join(", ", itemIds));
        }
    }

    internal async Task UpdateItemsWithIds(Gw2GizmosDbContext dbContext, int[] ids, CancellationToken stoppingToken)
    {
        Gw2Api.Contract.V2.Items.Item[]? apiItems = await _apiClient.V2.Items.GetByIds(ids, stoppingToken);

        // The API returns null (404 "all ids provided are invalid") when every requested id has
        // been removed from the game — common for the missing-item backfill, which retries ids
        // that are referenced elsewhere but no longer exist as items. Nothing to upsert.
        if (apiItems is null || apiItems.Length == 0)
        {
            _logger.LogWarning("Items API returned no data for {Count} id(s); they may have been removed.", ids.Length);
            return;
        }

        // Map the whole page up front so existence can be checked one query per type-group
        // (see BatchUpsert) instead of one query per item.
        var mapped = new List<Item>(apiItems.Length);
        foreach (Gw2Api.Contract.V2.Items.Item apiItem in apiItems)
        {
            try
            {
                Item? entity = ItemEntityMapper.MapApiItem(apiItem);
                if (entity != null)
                {
                    mapped.Add(entity);
                }
                else
                {
                    _logger.LogWarning("Unknown item type for ID {ItemId}: {Type}", apiItem.Id, apiItem.GetType());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing item ID {ItemId}", apiItem.Id);
            }
        }

        var newlyAddedIds = new List<int>();

        // One batched upsert per concrete type. Types with a 1:1 Details row include it so its
        // scalar values can be copied; Gizmo and simple Item have none.
        newlyAddedIds.AddRange(
            await BatchUpsert(
                dbContext,
                dbContext.Armors,
                mapped.OfType<Armor>().ToList(),
                q =>
                    q.Include(a => a.Details)
                        .ThenInclude(d => d.InfusionSlots)
                        .ThenInclude(s => s.Flags)
                        .Include(a => a.Details)
                        .ThenInclude(d => d.StatChoices)
                        .Include(a => a.Details)
                        .ThenInclude(d => d.InfixUpgrade!)
                        .ThenInclude(iu => iu.Attributes)
                        .Include(a => a.Details)
                        .ThenInclude(d => d.InfixUpgrade!)
                        .ThenInclude(iu => iu.Buff),
                (e, i) =>
                {
                    // Replace the whole Details subtree. The full graph is loaded above so EF
                    // tracks every child as a delete and orders the deletes before the inserts
                    // (the new subtree reuses the same ItemId/FKs), instead of relying on a DB
                    // cascade that doesn't fire for a same-PK 1:1 replacement.
                    dbContext.Remove(e.Details);
                    e.Details = i.Details;
                },
                stoppingToken
            )
        );
        newlyAddedIds.AddRange(
            await BatchUpsert(
                dbContext,
                dbContext.BackItems,
                mapped.OfType<BackItem>().ToList(),
                q =>
                    q.Include(b => b.Details)
                        .ThenInclude(d => d.InfusionSlots)
                        .ThenInclude(s => s.Flags)
                        .Include(b => b.Details)
                        .ThenInclude(d => d.StatChoices)
                        .Include(b => b.Details)
                        .ThenInclude(d => d.InfixUpgrade!)
                        .ThenInclude(iu => iu.Attributes)
                        .Include(b => b.Details)
                        .ThenInclude(d => d.InfixUpgrade!)
                        .ThenInclude(iu => iu.Buff),
                (e, i) =>
                {
                    dbContext.Remove(e.Details);
                    e.Details = i.Details;
                },
                stoppingToken
            )
        );
        newlyAddedIds.AddRange(
            await BatchUpsert(
                dbContext,
                dbContext.Bags,
                mapped.OfType<Bag>().ToList(),
                q => q.Include(b => b.Details),
                (e, i) => dbContext.Entry(e.Details).CurrentValues.SetValues(i.Details),
                stoppingToken
            )
        );
        newlyAddedIds.AddRange(
            await BatchUpsert(
                dbContext,
                dbContext.Consumables,
                mapped.OfType<Consumable>().ToList(),
                q =>
                    q.Include(c => c.Details)
                        .ThenInclude(d => d.ExtraRecipes)
                        .Include(c => c.Details)
                        .ThenInclude(d => d.Skins),
                (e, i) =>
                {
                    dbContext.Remove(e.Details);
                    e.Details = i.Details;
                },
                stoppingToken
            )
        );
        newlyAddedIds.AddRange(
            await BatchUpsert(
                dbContext,
                dbContext.Containers,
                mapped.OfType<Container>().ToList(),
                q => q.Include(c => c.Details),
                (e, i) => dbContext.Entry(e.Details).CurrentValues.SetValues(i.Details),
                stoppingToken
            )
        );
        newlyAddedIds.AddRange(
            await BatchUpsert(
                dbContext,
                dbContext.Gatherings,
                mapped.OfType<Gathering>().ToList(),
                q => q.Include(g => g.Details),
                (e, i) => dbContext.Entry(e.Details).CurrentValues.SetValues(i.Details),
                stoppingToken
            )
        );
        newlyAddedIds.AddRange(
            await BatchUpsert(
                dbContext,
                dbContext.MiniPets,
                mapped.OfType<MiniPet>().ToList(),
                q => q.Include(m => m.Details),
                (e, i) => dbContext.Entry(e.Details).CurrentValues.SetValues(i.Details),
                stoppingToken
            )
        );
        newlyAddedIds.AddRange(
            await BatchUpsert(
                dbContext,
                dbContext.Tools,
                mapped.OfType<Tool>().ToList(),
                q => q.Include(t => t.Details),
                (e, i) => dbContext.Entry(e.Details).CurrentValues.SetValues(i.Details),
                stoppingToken
            )
        );
        newlyAddedIds.AddRange(
            await BatchUpsert(
                dbContext,
                dbContext.Trinkets,
                mapped.OfType<Trinket>().ToList(),
                q =>
                    q.Include(t => t.Details)
                        .ThenInclude(d => d.InfusionSlots)
                        .ThenInclude(s => s.Flags)
                        .Include(t => t.Details)
                        .ThenInclude(d => d.StatChoices)
                        .Include(t => t.Details)
                        .ThenInclude(d => d.InfixUpgrade!)
                        .ThenInclude(iu => iu.Attributes)
                        .Include(t => t.Details)
                        .ThenInclude(d => d.InfixUpgrade!)
                        .ThenInclude(iu => iu.Buff),
                (e, i) =>
                {
                    dbContext.Remove(e.Details);
                    e.Details = i.Details;
                },
                stoppingToken
            )
        );
        newlyAddedIds.AddRange(
            await BatchUpsert(
                dbContext,
                dbContext.UpgradeComponents,
                mapped.OfType<UpgradeComponent>().ToList(),
                q =>
                    q.Include(u => u.Details)
                        .ThenInclude(d => d.InfixUpgrade!)
                        .ThenInclude(iu => iu.Attributes)
                        .Include(u => u.Details)
                        .ThenInclude(d => d.InfixUpgrade!)
                        .ThenInclude(iu => iu.Buff),
                (e, i) =>
                {
                    dbContext.Remove(e.Details);
                    e.Details = i.Details;
                },
                stoppingToken
            )
        );
        newlyAddedIds.AddRange(
            await BatchUpsert(
                dbContext,
                dbContext.Weapons,
                mapped.OfType<Weapon>().ToList(),
                q =>
                    q.Include(w => w.Details)
                        .ThenInclude(d => d.InfusionSlots)
                        .ThenInclude(s => s.Flags)
                        .Include(w => w.Details)
                        .ThenInclude(d => d.StatChoices)
                        .Include(w => w.Details)
                        .ThenInclude(d => d.InfixUpgrade!)
                        .ThenInclude(iu => iu.Attributes)
                        .Include(w => w.Details)
                        .ThenInclude(d => d.InfixUpgrade!)
                        .ThenInclude(iu => iu.Buff),
                (e, i) =>
                {
                    dbContext.Remove(e.Details);
                    e.Details = i.Details;
                },
                stoppingToken
            )
        );
        newlyAddedIds.AddRange(
            await BatchUpsert(dbContext, dbContext.Gizmos, mapped.OfType<Gizmo>().ToList(), null, null, stoppingToken)
        );

        // Simple items are the base type. OfType<Item>() would match every subtype (they all
        // derive from Item), so filter to the exact runtime type.
        newlyAddedIds.AddRange(
            await BatchUpsert(
                dbContext,
                dbContext.Items,
                mapped.Where(e => e.GetType() == typeof(Item)).ToList(),
                null,
                null,
                stoppingToken
            )
        );

        await dbContext.SaveChangesAsync(stoppingToken);

        if (newlyAddedIds.Count > 0)
        {
            _logger.LogInformation("Upserted {Count} newly-discovered item(s).", newlyAddedIds.Count);
        }
    }

    /// <summary>
    /// Upserts a batch of already-mapped entities of a single concrete type using one existence
    /// query for the whole batch. Returns the ids of rows that were newly inserted.
    /// </summary>
    private async Task<List<int>> BatchUpsert<TEntity>(
        Gw2GizmosDbContext dbContext,
        DbSet<TEntity> set,
        IReadOnlyList<TEntity> mapped,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? includeConfig,
        Action<TEntity, TEntity>? reconcileDetails,
        CancellationToken ct
    )
        where TEntity : Item
    {
        if (mapped.Count == 0)
        {
            return [];
        }

        List<int> ids = mapped.Select(e => e.Id).ToList();

        // Base Item collections are reconciled for every type, so always load them. Multiple
        // sibling collections → split query to avoid a cartesian product across the batch.
        IQueryable<TEntity> query = set.Include(e => e.Flags).Include(e => e.GameTypes).Include(e => e.Restrictions);
        if (includeConfig != null)
        {
            query = includeConfig(query);
        }
        Dictionary<int, TEntity> existingById = await query
            .AsSplitQuery()
            .Where(e => ids.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, ct);

        var newlyAddedIds = new List<int>();
        foreach (TEntity incoming in mapped)
        {
            if (existingById.TryGetValue(incoming.Id, out TEntity? existing))
            {
                dbContext.Entry(existing).CurrentValues.SetValues(incoming);

                // SetValues copies scalar columns only, never navigations, so child collections
                // are reconciled explicitly to match the API snapshot (full clear-and-re-add).
                ReplaceCollection(existing.Flags, incoming.Flags);
                ReplaceCollection(existing.GameTypes, incoming.GameTypes);
                ReplaceCollection(existing.Restrictions, incoming.Restrictions);

                // Details (scalars + its own nested collections) are reconciled per type.
                reconcileDetails?.Invoke(existing, incoming);
            }
            else
            {
                await set.AddAsync(incoming, ct);
                newlyAddedIds.Add(incoming.Id);
            }
        }

        return newlyAddedIds;
    }

    // Replaces a tracked child collection with the incoming snapshot: clearing orphans the old
    // rows (cascade-deleted via their required FK) and the incoming children (new, Id == 0) are
    // inserted. Used for the full-snapshot child collections that the API returns each refresh.
    private static void ReplaceCollection<TChild>(ICollection<TChild> existing, ICollection<TChild> incoming)
    {
        existing.Clear();
        foreach (TChild child in incoming)
        {
            existing.Add(child);
        }
    }
}