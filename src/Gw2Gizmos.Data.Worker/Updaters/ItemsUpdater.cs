using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Items;
using Gw2Gizmos.Gw2Api.Client;
using Microsoft.EntityFrameworkCore;

namespace Gw2Gizmos.Data.Worker.Updaters;

public class ItemsUpdater
{
    private readonly Gw2GizmosDbContext _dbContext;
    private readonly ILogger<Worker> _logger;
    private readonly Gw2ApiClient _apiClient;
    private const int PageSize = 50; // adjust as needed

    public ItemsUpdater(Gw2GizmosDbContext dbContext, IGw2ApiClientFactory apiClientFactory, ILogger<Worker> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
        _apiClient = apiClientFactory.Create(Locale.English);
    }

    public async Task UpdateItems(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting items update...");

        // Get all item IDs from API
        int[] allItemIds = await _apiClient.V2.Items.GetIds(stoppingToken);

        _logger.LogInformation("Starting item import. Total items to process: {Count}", allItemIds.Length);

        for (var i = 0; i < allItemIds.Length; i += PageSize)
        {
            try
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                _logger.LogInformation(
                    "Processing items {Start} to {End} of {Total}",
                    i + 1,
                    Math.Min(i + PageSize, allItemIds.Length),
                    allItemIds.Length
                );

                int[] pageIds = allItemIds.Skip(i).Take(PageSize).ToArray();
                Gw2Api.Contract.Items.Item[] apiItems = await _apiClient.V2.Items.GetByIds(pageIds, stoppingToken);

                foreach (Gw2Api.Contract.Items.Item apiItem in apiItems)
                {
                    try
                    {
                        // Determine type and map
                        if (apiItem is Gw2Api.Contract.Items.Armor apiArmor)
                        {
                            Armor armorEntity = MapToArmorEntity(apiArmor);
                            await AddOrUpdateArmor(armorEntity, stoppingToken);
                        }
                        else if (apiItem is Gw2Api.Contract.Items.BackItem apiBackItem)
                        {
                            BackItem backItemEntity = MapToBackItemEntity(apiBackItem);
                            await AddOrUpdateBackItem(backItemEntity, stoppingToken);
                        }
                        else if (apiItem is Gw2Api.Contract.Items.Bag apiBag)
                        {
                            Bag bagEntity = MapToBagEntity(apiBag);
                            await AddOrUpdateBag(bagEntity, stoppingToken);
                        }
                        else if (apiItem is Gw2Api.Contract.Items.Consumable apiConsumable)
                        {
                            Consumable consumableEntity = MapToConsumableEntity(apiConsumable);
                            await AddOrUpdateConsumable(consumableEntity, stoppingToken);
                        }
                        else if (apiItem is Gw2Api.Contract.Items.Container apiContainer)
                        {
                            Container containerEntity = MapToContainerEntity(apiContainer);
                            await AddOrUpdateContainer(containerEntity, stoppingToken);
                        }
                        else if (apiItem is Gw2Api.Contract.Items.Gathering apiGathering)
                        {
                            Gathering gatheringEntity = MapToGatheringEntity(apiGathering);
                            await AddOrUpdateGathering(gatheringEntity, stoppingToken);
                        }
                        else if (apiItem is Gw2Api.Contract.Items.Gizmo apiGizmo)
                        {
                            Gizmo gizmoEntity = await MapToGizmoEntity(apiGizmo);
                            await AddOrUpdateGizmo(gizmoEntity, stoppingToken);
                        }
                        else if (apiItem is Gw2Api.Contract.Items.MiniPet apiMiniPet)
                        {
                            MiniPet miniPetEntity = MapToMiniPetEntity(apiMiniPet);
                            await AddOrUpdateMiniPet(miniPetEntity, stoppingToken);
                        }
                        else if (apiItem is Gw2Api.Contract.Items.Tool apiTool)
                        {
                            Tool toolEntity = MapToToolEntity(apiTool);
                            await AddOrUpdateTool(toolEntity, stoppingToken);
                        }
                        else if (apiItem is Gw2Api.Contract.Items.Trinket apiTrinket)
                        {
                            Trinket trinketEntity = MapToTrinketEntity(apiTrinket);
                            await AddOrUpdateTrinket(trinketEntity, stoppingToken);
                        }
                        else if (apiItem is Gw2Api.Contract.Items.UpgradeComponent apiUpgradeComponent)
                        {
                            UpgradeComponent upgradeComponentEntity = await MapToUpgradeComponentEntity(
                                apiUpgradeComponent
                            );
                            await AddOrUpdateUpgradeComponent(upgradeComponentEntity, stoppingToken);
                        }
                        else if (apiItem is Gw2Api.Contract.Items.Weapon apiWeapon)
                        {
                            Weapon weaponEntity = await MapToWeaponEntity(apiWeapon);
                            await AddOrUpdateWeapon(weaponEntity, stoppingToken);
                        }
                        else
                        {
                            Item itemEntity = MapToItemEntity(apiItem);
                            await AddOrUpdateItem(itemEntity, stoppingToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing item ID {ItemId}", apiItem.Id);
                    }
                }

                await _dbContext.SaveChangesAsync(stoppingToken);

                _logger.LogInformation(
                    "Processed items {Start} to {End}. Total processed: {Total}",
                    i + 1,
                    Math.Min(i + PageSize, allItemIds.Length),
                    i + PageSize
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing items {Start} to {End}", i + 1, i + PageSize);
            }
        }

        _logger.LogInformation("Items update completed.");
    }

    #region Mapping Methods

    private static Item MapToItemEntity(Gw2Api.Contract.Items.Item apiItem)
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

    private static Armor MapToArmorEntity(Gw2Api.Contract.Items.Armor apiItem)
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
            Gw2Api.Contract.Items.InfixUpgrade? infix = apiItem.Details.InfixUpgrade;
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

    private static BackItem MapToBackItemEntity(Gw2Api.Contract.Items.BackItem apiItem)
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
            Gw2Api.Contract.Items.InfixUpgrade? infix = apiItem.Details.InfixUpgrade;
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

    private Bag MapToBagEntity(Gw2Api.Contract.Items.Bag apiItem)
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

    private Consumable MapToConsumableEntity(Gw2Api.Contract.Items.Consumable apiItem)
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

    private Container MapToContainerEntity(Gw2Api.Contract.Items.Container apiItem)
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

    private Gathering MapToGatheringEntity(Gw2Api.Contract.Items.Gathering apiItem)
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

    private static async Task<Gizmo> MapToGizmoEntity(Gw2Api.Contract.Items.Gizmo apiItem)
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

    private static MiniPet MapToMiniPetEntity(Gw2Api.Contract.Items.MiniPet apiItem)
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

    private static Tool MapToToolEntity(Gw2Api.Contract.Items.Tool apiItem)
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

    private static Trinket MapToTrinketEntity(Gw2Api.Contract.Items.Trinket apiItem)
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
            Gw2Api.Contract.Items.InfixUpgrade? infix = apiItem.Details.InfixUpgrade;
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

    private static async Task<UpgradeComponent> MapToUpgradeComponentEntity(
        Gw2Api.Contract.Items.UpgradeComponent apiItem
    )
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

    private static async Task<Weapon> MapToWeaponEntity(Gw2Api.Contract.Items.Weapon apiItem)
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
            Gw2Api.Contract.Items.InfixUpgrade? infix = apiItem.Details.InfixUpgrade;
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

    #region AddOrUpdate Methods

    private async Task AddOrUpdateArmor(Armor armor, CancellationToken ct)
    {
        Armor? existing = await _dbContext
            .Armors.Include(a => a.Details)
            .ThenInclude(d => d.InfusionSlots)
            .ThenInclude(s => s.Flags)
            .Include(a => a.Details.InfusionSlots)
            .ThenInclude(i => i.Flags)
            .Include(a => a.Details.StatChoices)
            .Include(w => w.Details.InfixUpgrade)
            .ThenInclude(i => i.Attributes)
            .Include(w => w.Details.InfixUpgrade.Buff)
            .FirstOrDefaultAsync(a => a.Id == armor.Id, ct);

        if (existing != null)
        {
            _dbContext.Entry(existing).CurrentValues.SetValues(armor);
            _dbContext.Entry(existing.Details).CurrentValues.SetValues(armor.Details);

            // TODO: handle properties nested in Details
        }
        else
        {
            await _dbContext.Armors.AddAsync(armor, ct);
        }
    }

    private async Task AddOrUpdateBackItem(BackItem backItem, CancellationToken ct)
    {
        BackItem? existing = await _dbContext
            .BackItems.Include(b => b.Details)
            .ThenInclude(d => d.InfusionSlots)
            .ThenInclude(s => s.Flags)
            .Include(b => b.Details.InfusionSlots)
            .ThenInclude(i => i.Flags)
            .Include(b => b.Details.StatChoices)
            .Include(w => w.Details.InfixUpgrade)
            .ThenInclude(i => i.Attributes)
            .Include(w => w.Details.InfixUpgrade.Buff)
            .FirstOrDefaultAsync(b => b.Id == backItem.Id, ct);

        if (existing != null)
        {
            _dbContext.Entry(existing).CurrentValues.SetValues(backItem);
            _dbContext.Entry(existing.Details).CurrentValues.SetValues(backItem.Details);

            // TODO: handle properties nested in Details
        }
        else
        {
            await _dbContext.BackItems.AddAsync(backItem, ct);
        }
    }

    private async Task AddOrUpdateBag(Bag bag, CancellationToken ct)
    {
        Bag? existing = await _dbContext.Bags.Include(b => b.Details).FirstOrDefaultAsync(b => b.Id == bag.Id, ct);

        if (existing != null)
        {
            _dbContext.Entry(existing).CurrentValues.SetValues(bag);
            _dbContext.Entry(existing.Details).CurrentValues.SetValues(bag.Details);

            // TODO: handle properties nested in Details
        }
        else
        {
            await _dbContext.Bags.AddAsync(bag, ct);
        }
    }

    private async Task AddOrUpdateConsumable(Consumable consumable, CancellationToken ct)
    {
        Consumable? existing = await _dbContext
            .Consumables.Include(c => c.Details)
            .ThenInclude(d => d.ExtraRecipes)
            .Include(c => c.Details.Skins)
            .FirstOrDefaultAsync(c => c.Id == consumable.Id, ct);

        if (existing != null)
        {
            _dbContext.Entry(existing).CurrentValues.SetValues(consumable);
            _dbContext.Entry(existing.Details).CurrentValues.SetValues(consumable.Details);
        }
        else
        {
            await _dbContext.Consumables.AddAsync(consumable, ct);
        }
    }

    private async Task AddOrUpdateContainer(Container container, CancellationToken ct)
    {
        Container? existing = await _dbContext
            .Containers.Include(c => c.Details)
            .FirstOrDefaultAsync(c => c.Id == container.Id, ct);

        if (existing != null)
        {
            _dbContext.Entry(existing).CurrentValues.SetValues(container);
            _dbContext.Entry(existing.Details).CurrentValues.SetValues(container.Details);
        }
        else
        {
            await _dbContext.Containers.AddAsync(container, ct);
        }
    }

    private async Task AddOrUpdateGathering(Gathering gathering, CancellationToken ct)
    {
        Gathering? existing = await _dbContext
            .Gatherings.Include(g => g.Details)
            .FirstOrDefaultAsync(g => g.Id == gathering.Id, ct);

        if (existing != null)
        {
            _dbContext.Entry(existing).CurrentValues.SetValues(gathering);
            _dbContext.Entry(existing.Details).CurrentValues.SetValues(gathering.Details);
        }
        else
        {
            await _dbContext.Gatherings.AddAsync(gathering, ct);
        }
    }

    private async Task AddOrUpdateGizmo(Gizmo gizmo, CancellationToken ct)
    {
        Gizmo? existing = await _dbContext.Gizmos.FirstOrDefaultAsync(g => g.Id == gizmo.Id, ct);
        if (existing != null)
        {
            _dbContext.Entry(existing).CurrentValues.SetValues(gizmo);
        }
        else
        {
            await _dbContext.Gizmos.AddAsync(gizmo, ct);
        }
    }

    private async Task AddOrUpdateMiniPet(MiniPet miniPet, CancellationToken ct)
    {
        MiniPet? existing = await _dbContext
            .MiniPets.Include(m => m.Details)
            .FirstOrDefaultAsync(t => t.Id == miniPet.Id, ct);
        if (existing != null)
        {
            _dbContext.Entry(existing).CurrentValues.SetValues(miniPet);
            _dbContext.Entry(existing.Details).CurrentValues.SetValues(miniPet.Details);
        }
        else
        {
            await _dbContext.MiniPets.AddAsync(miniPet, ct);
        }
    }

    private async Task AddOrUpdateTool(Tool tool, CancellationToken ct)
    {
        Tool? existing = await _dbContext.Tools.Include(t => t.Details).FirstOrDefaultAsync(t => t.Id == tool.Id, ct);
        if (existing != null)
        {
            _dbContext.Entry(existing).CurrentValues.SetValues(tool);
            _dbContext.Entry(existing.Details).CurrentValues.SetValues(tool.Details);
        }
        else
        {
            await _dbContext.Tools.AddAsync(tool, ct);
        }
    }

    private async Task AddOrUpdateTrinket(Trinket trinket, CancellationToken ct)
    {
        Trinket? existing = await _dbContext
            .Trinkets.Include(t => t.Details)
            .ThenInclude(d => d.InfusionSlots)
            .ThenInclude(s => s.Flags)
            .Include(t => t.Details.InfusionSlots)
            .ThenInclude(i => i.Flags)
            .Include(t => t.Details.StatChoices)
            .Include(w => w.Details.InfixUpgrade)
            .ThenInclude(i => i.Attributes)
            .Include(w => w.Details.InfixUpgrade.Buff)
            .FirstOrDefaultAsync(t => t.Id == trinket.Id, ct);

        if (existing != null)
        {
            _dbContext.Entry(existing).CurrentValues.SetValues(trinket);
            _dbContext.Entry(existing.Details).CurrentValues.SetValues(trinket.Details);

            // TODO: handle properties nested in Details
        }
        else
        {
            await _dbContext.Trinkets.AddAsync(trinket, ct);
        }
    }

    private async Task AddOrUpdateUpgradeComponent(UpgradeComponent upgradeComponent, CancellationToken ct)
    {
        UpgradeComponent? existing = await _dbContext
            .UpgradeComponents.Include(u => u.Details)
            .FirstOrDefaultAsync(u => u.Id == upgradeComponent.Id, ct);

        if (existing != null)
        {
            _dbContext.Entry(existing).CurrentValues.SetValues(upgradeComponent);
            _dbContext.Entry(existing.Details).CurrentValues.SetValues(upgradeComponent.Details);

            // TODO: handle properties nested in Details
        }
        else
        {
            await _dbContext.UpgradeComponents.AddAsync(upgradeComponent, ct);
        }
    }

    private async Task AddOrUpdateWeapon(Weapon weapon, CancellationToken ct)
    {
        Weapon? existing = await _dbContext
            .Weapons.Include(w => w.Details)
            .ThenInclude(d => d.InfusionSlots)
            .ThenInclude(s => s.Flags)
            .Include(a => a.Details.InfusionSlots)
            .ThenInclude(i => i.Flags)
            .Include(w => w.Details.StatChoices)
            .Include(w => w.Details.InfixUpgrade)
            .ThenInclude(i => i.Attributes)
            .Include(w => w.Details.InfixUpgrade.Buff)
            .FirstOrDefaultAsync(w => w.Id == weapon.Id, ct);

        if (existing != null)
        {
            _dbContext.Entry(existing).CurrentValues.SetValues(weapon);
            _dbContext.Entry(existing.Details).CurrentValues.SetValues(weapon.Details);
        }
        else
        {
            await _dbContext.Weapons.AddAsync(weapon, ct);
        }
    }

    private async Task AddOrUpdateItem(Item item, CancellationToken ct)
    {
        Item? existing = await _dbContext.Items.FirstOrDefaultAsync(i => i.Id == item.Id, ct);
        if (existing != null)
        {
            _dbContext.Entry(existing).CurrentValues.SetValues(item);
        }
        else
        {
            await _dbContext.Items.AddAsync(item, ct);
        }
    }

    #endregion
}
