using Gw2Gizmos.Data.EntityFramework.Entities.Items;

namespace Gw2Gizmos.Data.Worker.Updaters;

/// <summary>
/// Maps GW2 API item contracts to their EF entity types. Pure transforms — no DbContext or I/O.
/// </summary>
internal static class ItemEntityMapper
{
    public static Item? MapApiItem(Gw2Api.Contract.V2.Items.Item apiItem) =>
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
            _ => null,
        };

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
                        ItemId = s.ItemId,
                    })
                    .ToList(),
                StatChoices = apiItem
                    .Details.StatChoices.Select(s => new ArmorStatChoice { StatId = s, ArmorDetailsId = apiItem.Id })
                    .ToList(),
            },
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
                        Modifier = a.Modifier,
                    })
                    .ToList(),
                Buff =
                    infix.Buff == null
                        ? null
                        : new InfixUpgradeBuff { SkillId = infix.Buff.SkillId, Description = infix.Buff.Description },
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
                        BackItemDetailsId = apiItem.Id,
                    })
                    .ToList(),
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
                        Modifier = a.Modifier,
                    })
                    .ToList(),
                Buff =
                    infix.Buff == null
                        ? null
                        : new InfixUpgradeBuff { SkillId = infix.Buff.SkillId, Description = infix.Buff.Description },
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
                NoSellOrSort = apiItem.Details.NoSellOrSort,
            },
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
                            RecipeId = r,
                        })
                        .ToList()
                    ?? [],
                Skins =
                    apiItem
                        .Details.Skins?.Select(s => new ConsumableSkin { ConsumableId = apiItem.Id, SkinId = s })
                        .ToList()
                    ?? [],
            },
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
            Details = new ContainerDetails { ItemId = apiItem.Id, Type = apiItem.Details.Type.ToString() },
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
            Details = new GatheringDetails { ItemId = apiItem.Id, Type = apiItem.Details.Type.ToString() },
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
            Details = new MiniPetDetails { ItemId = apiItem.Id, MinipetId = apiItem.Details.MinipetId },
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
                Charges = apiItem.Details.Charges,
            },
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
                        TrinketDetailsId = apiItem.Id,
                    })
                    .ToList(),
            },
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
                        Modifier = a.Modifier,
                    })
                    .ToList(),
                Buff =
                    infix.Buff == null
                        ? null
                        : new InfixUpgradeBuff { SkillId = infix.Buff.SkillId, Description = infix.Buff.Description },
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
                InfixUpgrade =
                    apiItem.Details.InfixUpgrade == null
                        ? null
                        : new UpgradeComponentInfixUpgrade
                        {
                            Attributes = apiItem
                                .Details.InfixUpgrade.Attributes.Select(a => new InfixUpgradeAttribute
                                {
                                    Attribute = a.Attribute,
                                    Modifier = a.Modifier,
                                })
                                .ToList(),
                            Buff =
                                apiItem.Details.InfixUpgrade.Buff == null
                                    ? null
                                    : new InfixUpgradeBuff
                                    {
                                        SkillId = apiItem.Details.InfixUpgrade.Buff.SkillId,
                                        Description = apiItem.Details.InfixUpgrade.Buff.Description,
                                    },
                        },
            },
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
                        ItemId = s.ItemId,
                    })
                    .ToList(),
                StatChoices = apiItem
                    .Details.StatChoices.Select(s => new WeaponStatChoice { StatId = s, WeaponDetailsId = apiItem.Id })
                    .ToList(),
            },
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
                        Modifier = a.Modifier,
                    })
                    .ToList(),
                Buff =
                    infix.Buff == null
                        ? null
                        : new InfixUpgradeBuff { SkillId = infix.Buff.SkillId, Description = infix.Buff.Description },
            };
        }

        return entity;
    }
}
