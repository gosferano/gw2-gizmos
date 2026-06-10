using System.Threading.Channels;
using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Items;
using Gw2Gizmos.Gw2Api.Client;
using Microsoft.EntityFrameworkCore;

namespace Gw2Gizmos.Data.Worker.Updaters;

/// <summary>
/// Item master-data ingester. The scheduled refresh (<see cref="UpdateItems"/>) only *enqueues*
/// item ids onto the items channel; the single consumer (<see cref="ItemsMissingUpdater"/>) is the
/// only path that calls <see cref="UpdateItemsWithIds"/> to fetch and upsert them. Because there is
/// one consumer, the scheduled refresh and the commerce-driven backfill (which also enqueues ids)
/// can never insert the same id concurrently. Newly-added items are signalled on the
/// <see cref="ItemAddedDto"/> channel so commerce data can be backfilled for them.
/// </summary>
public class ItemsUpdater
{
    private readonly Gw2GizmosDbContext _dbContext;
    private readonly ILogger<ItemsUpdater> _logger;
    private readonly ChannelWriter<ItemAddedDto> _itemsAddedWriter;
    private readonly ChannelWriter<ItemMissingDto> _itemsMissingWriter;
    private readonly Gw2ApiClient _apiClient;

    public ItemsUpdater(
        Gw2GizmosDbContext dbContext,
        IGw2ApiClientFactory apiClientFactory,
        ILogger<ItemsUpdater> logger,
        Channel<ItemAddedDto> itemsAdded,
        Channel<ItemMissingDto> itemsMissing
    )
    {
        _dbContext = dbContext;
        _logger = logger;
        _itemsAddedWriter = itemsAdded.Writer;
        _itemsMissingWriter = itemsMissing.Writer;
        _apiClient = apiClientFactory.Create(Locale.English);
    }

    /// <summary>
    /// Enqueues every item id onto the items channel for the single consumer to upsert, rather than
    /// writing directly — so the scheduled refresh never races the commerce-driven backfill.
    /// </summary>
    public async Task UpdateItems(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting items update...");

        // Get all item IDs from API
        int[]? allItemIds = await _apiClient.V2.Items.GetIds(stoppingToken);

        if (allItemIds is null || allItemIds.Length == 0)
        {
            _logger.LogWarning("Items API returned no ids; skipping items update.");
            return;
        }

        _logger.LogInformation("Queueing {Count} item ids for upsert.", allItemIds.Length);

        foreach (int itemId in allItemIds)
        {
            await _itemsMissingWriter.WriteAsync(new ItemMissingDto { ItemId = itemId }, stoppingToken);
        }

        _logger.LogInformation("Items update queued.");
    }

    internal async Task UpdateItemsWithIds(int[] ids, CancellationToken stoppingToken)
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
                Item? entity = MapApiItem(apiItem);
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
                _dbContext.Armors,
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
                    _dbContext.Remove(e.Details);
                    e.Details = i.Details;
                },
                stoppingToken
            )
        );
        newlyAddedIds.AddRange(
            await BatchUpsert(
                _dbContext.BackItems,
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
                    _dbContext.Remove(e.Details);
                    e.Details = i.Details;
                },
                stoppingToken
            )
        );
        newlyAddedIds.AddRange(
            await BatchUpsert(
                _dbContext.Bags,
                mapped.OfType<Bag>().ToList(),
                q => q.Include(b => b.Details),
                (e, i) => _dbContext.Entry(e.Details).CurrentValues.SetValues(i.Details),
                stoppingToken
            )
        );
        newlyAddedIds.AddRange(
            await BatchUpsert(
                _dbContext.Consumables,
                mapped.OfType<Consumable>().ToList(),
                q =>
                    q.Include(c => c.Details)
                        .ThenInclude(d => d.ExtraRecipes)
                        .Include(c => c.Details)
                        .ThenInclude(d => d.Skins),
                (e, i) =>
                {
                    _dbContext.Remove(e.Details);
                    e.Details = i.Details;
                },
                stoppingToken
            )
        );
        newlyAddedIds.AddRange(
            await BatchUpsert(
                _dbContext.Containers,
                mapped.OfType<Container>().ToList(),
                q => q.Include(c => c.Details),
                (e, i) => _dbContext.Entry(e.Details).CurrentValues.SetValues(i.Details),
                stoppingToken
            )
        );
        newlyAddedIds.AddRange(
            await BatchUpsert(
                _dbContext.Gatherings,
                mapped.OfType<Gathering>().ToList(),
                q => q.Include(g => g.Details),
                (e, i) => _dbContext.Entry(e.Details).CurrentValues.SetValues(i.Details),
                stoppingToken
            )
        );
        newlyAddedIds.AddRange(
            await BatchUpsert(
                _dbContext.MiniPets,
                mapped.OfType<MiniPet>().ToList(),
                q => q.Include(m => m.Details),
                (e, i) => _dbContext.Entry(e.Details).CurrentValues.SetValues(i.Details),
                stoppingToken
            )
        );
        newlyAddedIds.AddRange(
            await BatchUpsert(
                _dbContext.Tools,
                mapped.OfType<Tool>().ToList(),
                q => q.Include(t => t.Details),
                (e, i) => _dbContext.Entry(e.Details).CurrentValues.SetValues(i.Details),
                stoppingToken
            )
        );
        newlyAddedIds.AddRange(
            await BatchUpsert(
                _dbContext.Trinkets,
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
                    _dbContext.Remove(e.Details);
                    e.Details = i.Details;
                },
                stoppingToken
            )
        );
        newlyAddedIds.AddRange(
            await BatchUpsert(
                _dbContext.UpgradeComponents,
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
                    _dbContext.Remove(e.Details);
                    e.Details = i.Details;
                },
                stoppingToken
            )
        );
        newlyAddedIds.AddRange(
            await BatchUpsert(
                _dbContext.Weapons,
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
                    _dbContext.Remove(e.Details);
                    e.Details = i.Details;
                },
                stoppingToken
            )
        );
        newlyAddedIds.AddRange(
            await BatchUpsert(_dbContext.Gizmos, mapped.OfType<Gizmo>().ToList(), null, null, stoppingToken)
        );

        // Simple items are the base type. OfType<Item>() would match every subtype (they all
        // derive from Item), so filter to the exact runtime type.
        newlyAddedIds.AddRange(
            await BatchUpsert(
                _dbContext.Items,
                mapped.Where(e => e.GetType() == typeof(Item)).ToList(),
                null,
                null,
                stoppingToken
            )
        );

        await _dbContext.SaveChangesAsync(stoppingToken);

        // Signal newly-added items only after the page has been persisted, so a failed save
        // never emits a phantom "added" event.
        foreach (int id in newlyAddedIds)
        {
            await _itemsAddedWriter.WriteAsync(new ItemAddedDto { ItemId = id }, stoppingToken);
        }
    }

    private static Item? MapApiItem(Gw2Api.Contract.V2.Items.Item apiItem) =>
        apiItem switch
        {
            Gw2Api.Contract.V2.Items.Armor a => MapToArmorEntity(a),
            Gw2Api.Contract.V2.Items.BackItem b => MapToBackItemEntity(b),
            Gw2Api.Contract.V2.Items.Bag b => MapToBagEntity(b),
            Gw2Api.Contract.V2.Items.Consumable c => MapToConsumableEntity(c),
            Gw2Api.Contract.V2.Items.Container c => MapToContainerEntity(c),
            Gw2Api.Contract.V2.Items.Gathering g => MapToGatheringEntity(g),
            Gw2Api.Contract.V2.Items.Gizmo g => MapToGizmoEntity(g),
            Gw2Api.Contract.V2.Items.MiniPet m => MapToMiniPetEntity(m),
            Gw2Api.Contract.V2.Items.Tool t => MapToToolEntity(t),
            Gw2Api.Contract.V2.Items.Trinket t => MapToTrinketEntity(t),
            Gw2Api.Contract.V2.Items.UpgradeComponent u => MapToUpgradeComponentEntity(u),
            Gw2Api.Contract.V2.Items.Weapon w => MapToWeaponEntity(w),
            Gw2Api.Contract.V2.Items.ItemSimple s => MapToItemEntity(s),
            _ => null
        };

    /// <summary>
    /// Upserts a batch of already-mapped entities of a single concrete type using one existence
    /// query for the whole batch. Returns the ids of rows that were newly inserted.
    /// </summary>
    private async Task<List<int>> BatchUpsert<TEntity>(
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
                _dbContext.Entry(existing).CurrentValues.SetValues(incoming);

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

    #region Mapping Methods

    private static Item MapToItemEntity(Gw2Api.Contract.V2.Items.ItemSimple apiItem)
    {
        return new Item
        {
            Id = apiItem.Id,
            Name = apiItem.Name,
            ChatLink = apiItem.ChatLink,
            Type = apiItem.Type,
            Rarity = apiItem.Rarity,
            Level = apiItem.Level,
            VendorValue = apiItem.VendorValue,
            Icon = apiItem.Icon,
            Description = apiItem.Description,
            Flags = apiItem.Flags.Select(f => new ItemFlag { ItemId = apiItem.Id, Value = f.Value }).ToList(),
            GameTypes = apiItem
                .GameTypes.Select(gt => new ItemGameType { ItemId = apiItem.Id, Value = gt.Value })
                .ToList(),
            Restrictions = apiItem
                .Restrictions.Select(r => new ItemRestriction() { ItemId = apiItem.Id, Value = r.Value })
                .ToList(),
        };
    }

    private static Armor MapToArmorEntity(Gw2Api.Contract.V2.Items.Armor apiItem)
    {
        var entity = new Armor
        {
            Id = apiItem.Id,
            Name = apiItem.Name,
            ChatLink = apiItem.ChatLink,
            Type = apiItem.Type,
            Rarity = apiItem.Rarity,
            Level = apiItem.Level,
            VendorValue = apiItem.VendorValue,
            Icon = apiItem.Icon,
            Description = apiItem.Description,
            Flags = apiItem.Flags.Select(f => new ItemFlag { ItemId = apiItem.Id, Value = f.Value }).ToList(),
            GameTypes = apiItem
                .GameTypes.Select(gt => new ItemGameType { ItemId = apiItem.Id, Value = gt.Value })
                .ToList(),
            Restrictions = apiItem
                .Restrictions.Select(r => new ItemRestriction() { ItemId = apiItem.Id, Value = r.Value })
                .ToList(),
            Details = new ArmorDetails
            {
                ItemId = apiItem.Id,
                Type = apiItem.Details.Type,
                WeightClass = apiItem.Details.WeightClass,
                Defense = apiItem.Details.Defense,
                AttributeAdjustment = apiItem.Details.AttributeAdjustment,
                SuffixItemId = apiItem.Details.SuffixItemId,
                SecondarySuffixItemId = apiItem.Details.SecondarySuffixItemId,
                InfusionSlots = apiItem
                    .Details.InfusionSlots.Select(s => new ArmorInfusionSlot
                    {
                        ArmorDetailsId = apiItem.Id,
                        Flags = s.Flags.Select(f => new ItemInfusionSlotFlag { Flag = f.Value }).ToList(),
                        ItemId = s.ItemId
                    })
                    .ToList(),
                StatChoices = apiItem
                    .Details.StatChoices.Select(s => new ArmorStatChoice { StatId = s, ArmorDetailsId = apiItem.Id })
                    .ToList()
            }
        };

        // Map InfixUpgrade if exists
        if (apiItem.Details.InfixUpgrade != null)
        {
            Gw2Api.Contract.V2.Items.InfixUpgrade? infix = apiItem.Details.InfixUpgrade;
            entity.Details.InfixUpgrade = new ArmorInfixUpgrade
            {
                Attributes = infix
                    .Attributes.Select(a => new InfixUpgradeAttribute
                    {
                        Attribute = a.Attribute,
                        Modifier = a.Modifier
                    })
                    .ToList(),
                Buff =
                    infix.Buff == null
                        ? null
                        : new InfixUpgradeBuff { SkillId = infix.Buff.SkillId, Description = infix.Buff.Description }
            };
        }

        return entity;
    }

    private static BackItem MapToBackItemEntity(Gw2Api.Contract.V2.Items.BackItem apiItem)
    {
        var entity = new BackItem
        {
            Id = apiItem.Id,
            Name = apiItem.Name,
            ChatLink = apiItem.ChatLink,
            Type = apiItem.Type,
            Rarity = apiItem.Rarity,
            Level = apiItem.Level,
            VendorValue = apiItem.VendorValue,
            Icon = apiItem.Icon,
            Description = apiItem.Description,
            Flags = apiItem.Flags.Select(f => new ItemFlag { ItemId = apiItem.Id, Value = f.Value }).ToList(),
            GameTypes = apiItem
                .GameTypes.Select(gt => new ItemGameType { ItemId = apiItem.Id, Value = gt.Value })
                .ToList(),
            Restrictions = apiItem
                .Restrictions.Select(r => new ItemRestriction() { ItemId = apiItem.Id, Value = r.Value })
                .ToList(),
            Details = new BackItemDetails
            {
                ItemId = apiItem.Id,
                AttributeAdjustment = apiItem.Details.AttributeAdjustment,
                SuffixItemId = apiItem.Details.SuffixItemId,
                SecondarySuffixItemId = apiItem.Details.SecondarySuffixItemId,
                InfusionSlots = apiItem
                    .Details.InfusionSlots.Select(s => new BackItemInfusionSlot
                    {
                        BackItemDetailsId = apiItem.Id,
                        Flags = s.Flags.Select(f => new ItemInfusionSlotFlag { Flag = f.Value }).ToList(),
                        ItemId = s.ItemId,
                    })
                    .ToList(),
                StatChoices = apiItem
                    .Details.StatChoices.Select(s => new BackItemStatChoice
                    {
                        StatId = s,
                        BackItemDetailsId = apiItem.Id
                    })
                    .ToList()
            },
        };

        // Map InfixUpgrade if exists
        if (apiItem.Details.InfixUpgrade != null)
        {
            Gw2Api.Contract.V2.Items.InfixUpgrade? infix = apiItem.Details.InfixUpgrade;
            entity.Details.InfixUpgrade = new BackItemInfixUpgrade
            {
                Attributes = infix
                    .Attributes.Select(a => new InfixUpgradeAttribute
                    {
                        Attribute = a.Attribute,
                        Modifier = a.Modifier
                    })
                    .ToList(),
                Buff =
                    infix.Buff == null
                        ? null
                        : new InfixUpgradeBuff { SkillId = infix.Buff.SkillId, Description = infix.Buff.Description }
            };
        }

        return entity;
    }

    private static Bag MapToBagEntity(Gw2Api.Contract.V2.Items.Bag apiItem)
    {
        return new Bag
        {
            Id = apiItem.Id,
            Name = apiItem.Name,
            ChatLink = apiItem.ChatLink,
            Type = apiItem.Type,
            Rarity = apiItem.Rarity,
            Level = apiItem.Level,
            VendorValue = apiItem.VendorValue,
            Icon = apiItem.Icon,
            Description = apiItem.Description,
            Flags = apiItem.Flags.Select(f => new ItemFlag { ItemId = apiItem.Id, Value = f.Value }).ToList(),
            GameTypes = apiItem
                .GameTypes.Select(gt => new ItemGameType { ItemId = apiItem.Id, Value = gt.Value })
                .ToList(),
            Restrictions = apiItem
                .Restrictions.Select(r => new ItemRestriction() { ItemId = apiItem.Id, Value = r.Value })
                .ToList(),
            Details = new BagDetails
            {
                ItemId = apiItem.Id,
                Size = apiItem.Details.Size,
                NoSellOrSort = apiItem.Details.NoSellOrSort
            }
        };
    }

    private static Consumable MapToConsumableEntity(Gw2Api.Contract.V2.Items.Consumable apiItem)
    {
        return new Consumable
        {
            Id = apiItem.Id,
            Name = apiItem.Name,
            ChatLink = apiItem.ChatLink,
            Type = apiItem.Type,
            Rarity = apiItem.Rarity,
            Level = apiItem.Level,
            VendorValue = apiItem.VendorValue,
            Icon = apiItem.Icon,
            Description = apiItem.Description,
            Flags = apiItem.Flags.Select(f => new ItemFlag { ItemId = apiItem.Id, Value = f.Value }).ToList(),
            GameTypes = apiItem
                .GameTypes.Select(gt => new ItemGameType { ItemId = apiItem.Id, Value = gt.Value })
                .ToList(),
            Restrictions = apiItem
                .Restrictions.Select(r => new ItemRestriction() { ItemId = apiItem.Id, Value = r.Value })
                .ToList(),
            Details = new ConsumableDetails
            {
                ItemId = apiItem.Id,
                Type = apiItem.Details.Type.ToString(),
                Description = apiItem.Details.Description,
                DurationMs = apiItem.Details.DurationMs,
                UnlockType = apiItem.Details.UnlockType.ToString(),
                ColorId = apiItem.Details.ColorId,
                RecipeId = apiItem.Details.RecipeId,
                GuildUpgradeId = apiItem.Details.GuildUpgradeId,
                ApplyCount = apiItem.Details.ApplyCount,
                Name = apiItem.Details.Name,
                Icon = apiItem.Details.Icon,
                ExtraRecipes =
                    apiItem
                        .Details.ExtraRecipeIds?.Select(r => new ConsumableExtraRecipe
                        {
                            ConsumableId = apiItem.Id,
                            RecipeId = r
                        })
                        .ToList() ?? [],
                Skins =
                    apiItem
                        .Details.Skins?.Select(s => new ConsumableSkin { ConsumableId = apiItem.Id, SkinId = s })
                        .ToList() ?? []
            }
        };
    }

    private static Container MapToContainerEntity(Gw2Api.Contract.V2.Items.Container apiItem)
    {
        return new Container
        {
            Id = apiItem.Id,
            Name = apiItem.Name,
            ChatLink = apiItem.ChatLink,
            Type = apiItem.Type,
            Rarity = apiItem.Rarity,
            Level = apiItem.Level,
            VendorValue = apiItem.VendorValue,
            Icon = apiItem.Icon,
            Description = apiItem.Description,
            Flags = apiItem.Flags.Select(f => new ItemFlag { ItemId = apiItem.Id, Value = f.Value }).ToList(),
            GameTypes = apiItem
                .GameTypes.Select(gt => new ItemGameType { ItemId = apiItem.Id, Value = gt.Value })
                .ToList(),
            Restrictions = apiItem
                .Restrictions.Select(r => new ItemRestriction() { ItemId = apiItem.Id, Value = r.Value })
                .ToList(),
            Details = new ContainerDetails { ItemId = apiItem.Id, Type = apiItem.Details.Type.ToString() }
        };
    }

    private static Gathering MapToGatheringEntity(Gw2Api.Contract.V2.Items.Gathering apiItem)
    {
        return new Gathering
        {
            Id = apiItem.Id,
            Name = apiItem.Name,
            ChatLink = apiItem.ChatLink,
            Type = apiItem.Type,
            Rarity = apiItem.Rarity,
            Level = apiItem.Level,
            VendorValue = apiItem.VendorValue,
            Icon = apiItem.Icon,
            Description = apiItem.Description,
            Flags = apiItem.Flags.Select(f => new ItemFlag { ItemId = apiItem.Id, Value = f.Value }).ToList(),
            GameTypes = apiItem
                .GameTypes.Select(gt => new ItemGameType { ItemId = apiItem.Id, Value = gt.Value })
                .ToList(),
            Restrictions = apiItem
                .Restrictions.Select(r => new ItemRestriction() { ItemId = apiItem.Id, Value = r.Value })
                .ToList(),
            Details = new GatheringDetails { ItemId = apiItem.Id, Type = apiItem.Details.Type.ToString(), }
        };
    }

    private static Gizmo MapToGizmoEntity(Gw2Api.Contract.V2.Items.Gizmo apiItem)
    {
        var entity = new Gizmo
        {
            Id = apiItem.Id,
            Name = apiItem.Name,
            ChatLink = apiItem.ChatLink,
            Type = apiItem.Type,
            Rarity = apiItem.Rarity,
            Level = apiItem.Level,
            VendorValue = apiItem.VendorValue,
            Icon = apiItem.Icon,
            Description = apiItem.Description,
            Flags = apiItem.Flags.Select(f => new ItemFlag { ItemId = apiItem.Id, Value = f.Value }).ToList(),
            GameTypes = apiItem
                .GameTypes.Select(gt => new ItemGameType { ItemId = apiItem.Id, Value = gt.Value })
                .ToList(),
            Restrictions = apiItem
                .Restrictions.Select(r => new ItemRestriction() { ItemId = apiItem.Id, Value = r.Value })
                .ToList(),
        };

        return entity;
    }

    private static MiniPet MapToMiniPetEntity(Gw2Api.Contract.V2.Items.MiniPet apiItem)
    {
        return new MiniPet
        {
            Id = apiItem.Id,
            Name = apiItem.Name,
            ChatLink = apiItem.ChatLink,
            Type = apiItem.Type,
            Rarity = apiItem.Rarity,
            Level = apiItem.Level,
            VendorValue = apiItem.VendorValue,
            Icon = apiItem.Icon,
            Description = apiItem.Description,
            Flags = apiItem.Flags.Select(f => new ItemFlag { ItemId = apiItem.Id, Value = f.Value }).ToList(),
            GameTypes = apiItem
                .GameTypes.Select(gt => new ItemGameType { ItemId = apiItem.Id, Value = gt.Value })
                .ToList(),
            Restrictions = apiItem
                .Restrictions.Select(r => new ItemRestriction() { ItemId = apiItem.Id, Value = r.Value })
                .ToList(),
            Details = new MiniPetDetails { ItemId = apiItem.Id, MinipetId = apiItem.Details.MinipetId, }
        };
    }

    private static Tool MapToToolEntity(Gw2Api.Contract.V2.Items.Tool apiItem)
    {
        return new Tool
        {
            Id = apiItem.Id,
            Name = apiItem.Name,
            ChatLink = apiItem.ChatLink,
            Type = apiItem.Type,
            Rarity = apiItem.Rarity,
            Level = apiItem.Level,
            VendorValue = apiItem.VendorValue,
            Icon = apiItem.Icon,
            Description = apiItem.Description,
            Flags = apiItem.Flags.Select(f => new ItemFlag { ItemId = apiItem.Id, Value = f.Value }).ToList(),
            GameTypes = apiItem
                .GameTypes.Select(gt => new ItemGameType { ItemId = apiItem.Id, Value = gt.Value })
                .ToList(),
            Restrictions = apiItem
                .Restrictions.Select(r => new ItemRestriction() { ItemId = apiItem.Id, Value = r.Value })
                .ToList(),
            Details = new ToolDetails
            {
                ItemId = apiItem.Id,
                Type = apiItem.Details.Type,
                Charges = apiItem.Details.Charges
            }
        };
    }

    private static Trinket MapToTrinketEntity(Gw2Api.Contract.V2.Items.Trinket apiItem)
    {
        var entity = new Trinket
        {
            Id = apiItem.Id,
            Name = apiItem.Name,
            ChatLink = apiItem.ChatLink,
            Type = apiItem.Type,
            Rarity = apiItem.Rarity,
            Level = apiItem.Level,
            VendorValue = apiItem.VendorValue,
            Icon = apiItem.Icon,
            Description = apiItem.Description,
            Flags = apiItem.Flags.Select(f => new ItemFlag { ItemId = apiItem.Id, Value = f.Value }).ToList(),
            GameTypes = apiItem
                .GameTypes.Select(gt => new ItemGameType { ItemId = apiItem.Id, Value = gt.Value })
                .ToList(),
            Restrictions = apiItem
                .Restrictions.Select(r => new ItemRestriction() { ItemId = apiItem.Id, Value = r.Value })
                .ToList(),
            Details = new TrinketDetails
            {
                ItemId = apiItem.Id,
                Type = apiItem.Details.Type.ToString(),
                AttributeAdjustment = apiItem.Details.AttributeAdjustment,
                SuffixItemId = apiItem.Details.SuffixItemId,
                SecondarySuffixItemId = apiItem.Details.SecondarySuffixItemId,
                InfusionSlots = apiItem
                    .Details.InfusionSlots.Select(s => new TrinketInfusionSlot
                    {
                        TrinketDetailsId = apiItem.Id,
                        Flags = s.Flags.Select(f => new ItemInfusionSlotFlag { Flag = f.Value }).ToList(),
                        ItemId = s.ItemId,
                    })
                    .ToList(),
                StatChoices = apiItem
                    .Details.StatChoices.Select(s => new TrinketStatChoice
                    {
                        StatId = s,
                        TrinketDetailsId = apiItem.Id
                    })
                    .ToList()
            }
        };

        // Map InfixUpgrade if exists
        if (apiItem.Details.InfixUpgrade != null)
        {
            Gw2Api.Contract.V2.Items.InfixUpgrade? infix = apiItem.Details.InfixUpgrade;
            entity.Details.InfixUpgrade = new TrinketInfixUpgrade
            {
                Attributes = infix
                    .Attributes.Select(a => new InfixUpgradeAttribute
                    {
                        Attribute = a.Attribute,
                        Modifier = a.Modifier
                    })
                    .ToList(),
                Buff =
                    infix.Buff == null
                        ? null
                        : new InfixUpgradeBuff { SkillId = infix.Buff.SkillId, Description = infix.Buff.Description }
            };
        }

        return entity;
    }

    private static UpgradeComponent MapToUpgradeComponentEntity(Gw2Api.Contract.V2.Items.UpgradeComponent apiItem)
    {
        var entity = new UpgradeComponent
        {
            Id = apiItem.Id,
            Name = apiItem.Name,
            ChatLink = apiItem.ChatLink,
            Type = apiItem.Type,
            Rarity = apiItem.Rarity,
            Level = apiItem.Level,
            VendorValue = apiItem.VendorValue,
            Icon = apiItem.Icon,
            Description = apiItem.Description,
            Flags = apiItem.Flags.Select(f => new ItemFlag { ItemId = apiItem.Id, Value = f.Value }).ToList(),
            GameTypes = apiItem
                .GameTypes.Select(gt => new ItemGameType { ItemId = apiItem.Id, Value = gt.Value })
                .ToList(),
            Restrictions = apiItem
                .Restrictions.Select(r => new ItemRestriction() { ItemId = apiItem.Id, Value = r.Value })
                .ToList(),
            Details = new UpgradeComponentDetails
            {
                ItemId = apiItem.Id,
                Type = apiItem.Details.Type.ToString(),
                Suffix = apiItem.Details.Suffix,
                Flags = apiItem.Details.Flags.Select(f => f.Value).ToList(),
                InfusionUpgradeFlags = apiItem.Details.InfusionUpgradeFlags.Select(f => f.Value).ToList(),
                Bonuses = apiItem.Details.Bonuses?.ToList() ?? [],
                InfixUpgrade = new UpgradeComponentInfixUpgrade
                {
                    Attributes = apiItem
                        .Details.InfixUpgrade.Attributes.Select(a => new InfixUpgradeAttribute
                        {
                            Attribute = a.Attribute,
                            Modifier = a.Modifier
                        })
                        .ToList(),
                    Buff =
                        apiItem.Details.InfixUpgrade.Buff == null
                            ? null
                            : new InfixUpgradeBuff
                            {
                                SkillId = apiItem.Details.InfixUpgrade.Buff.SkillId,
                                Description = apiItem.Details.InfixUpgrade.Buff.Description
                            }
                }
            }
        };

        return entity;
    }

    private static Weapon MapToWeaponEntity(Gw2Api.Contract.V2.Items.Weapon apiItem)
    {
        var entity = new Weapon
        {
            Id = apiItem.Id,
            Name = apiItem.Name,
            ChatLink = apiItem.ChatLink,
            Type = apiItem.Type,
            Rarity = apiItem.Rarity,
            Level = apiItem.Level,
            VendorValue = apiItem.VendorValue,
            Icon = apiItem.Icon,
            Description = apiItem.Description,
            Flags = apiItem.Flags.Select(f => new ItemFlag { ItemId = apiItem.Id, Value = f.Value }).ToList(),
            GameTypes = apiItem
                .GameTypes.Select(gt => new ItemGameType { ItemId = apiItem.Id, Value = gt.Value })
                .ToList(),
            Restrictions = apiItem
                .Restrictions.Select(r => new ItemRestriction() { ItemId = apiItem.Id, Value = r.Value })
                .ToList(),
            Details = new WeaponDetails
            {
                ItemId = apiItem.Id,
                Type = apiItem.Details.Type.ToString(),
                DamageType = apiItem.Details.DamageType.ToString(),
                MinPower = apiItem.Details.MinPower,
                MaxPower = apiItem.Details.MaxPower,
                Defense = apiItem.Details.Defense,
                AttributeAdjustment = apiItem.Details.AttributeAdjustment,
                SuffixItemId = apiItem.Details.SuffixItemId,
                SecondarySuffixItemId = apiItem.Details.SecondarySuffixItemId,
                InfusionSlots = apiItem
                    .Details.InfusionSlots.Select(s => new WeaponInfusionSlot
                    {
                        WeaponDetailsId = apiItem.Id,
                        Flags = s.Flags.Select(f => new ItemInfusionSlotFlag { Flag = f.Value }).ToList(),
                        ItemId = s.ItemId
                    })
                    .ToList(),
                StatChoices = apiItem
                    .Details.StatChoices.Select(s => new WeaponStatChoice { StatId = s, WeaponDetailsId = apiItem.Id })
                    .ToList()
            }
        };

        // Map InfixUpgrade if exists
        if (apiItem.Details.InfixUpgrade != null)
        {
            Gw2Api.Contract.V2.Items.InfixUpgrade? infix = apiItem.Details.InfixUpgrade;
            entity.Details.InfixUpgrade = new WeaponInfixUpgrade
            {
                Attributes = infix
                    .Attributes.Select(a => new InfixUpgradeAttribute
                    {
                        Attribute = a.Attribute,
                        Modifier = a.Modifier
                    })
                    .ToList(),
                Buff =
                    infix.Buff == null
                        ? null
                        : new InfixUpgradeBuff { SkillId = infix.Buff.SkillId, Description = infix.Buff.Description }
            };
        }

        return entity;
    }

    #endregion

}
