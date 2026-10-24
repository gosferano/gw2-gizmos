using System.Text.Json;
using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Wardrobe;
using Gw2Gizmos.Gw2Api.Client;
using Gw2Gizmos.Gw2Api.Contract;
using Microsoft.EntityFrameworkCore;
using ApiFinishers = Gw2Gizmos.Gw2Api.Contract.V2.Finishers;
using ApiGliders = Gw2Gizmos.Gw2Api.Contract.V2.Gliders;
using ApiJadeBots = Gw2Gizmos.Gw2Api.Contract.V2.JadeBots;
using ApiMailCarriers = Gw2Gizmos.Gw2Api.Contract.V2.MailCarriers;
using ApiMinis = Gw2Gizmos.Gw2Api.Contract.V2.Minis;
using ApiNovelties = Gw2Gizmos.Gw2Api.Contract.V2.Novelties;
using ApiOutfits = Gw2Gizmos.Gw2Api.Contract.V2.Outfits;
using ApiSkins = Gw2Gizmos.Gw2Api.Contract.V2.Skins;

namespace Gw2Gizmos.Data.Worker.Updaters;

/// <summary>
/// Full-refresh ingester for the wardrobe: every skin (<c>/v2/skins</c>, about 10,700, fetched 200 at a time) with its
/// type-specific details, and every miniature (<c>/v2/minis</c>); and the items that unlock each novelty, outfit,
/// finisher, mail carrier, glider and jade bot (<see cref="UnlockItem"/>). All public, changing only with game
/// releases, so it runs daily. Skins and minis the API stops listing are kept.
/// </summary>
public class WardrobeUpdater
{
    private const int PageSize = 200;

    private readonly Gw2GizmosDbContext _dbContext;
    private readonly Gw2ApiClient _apiClient;
    private readonly ILogger<WardrobeUpdater> _logger;

    public WardrobeUpdater(
        Gw2GizmosDbContext dbContext,
        IGw2ApiClientFactory apiClientFactory,
        ILogger<WardrobeUpdater> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
        _apiClient = apiClientFactory.Create(Locale.English);
    }

    public async Task UpdateWardrobe(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting wardrobe update...");

        int[]? skinIds = await _apiClient.V2.Skins.GetIds(stoppingToken);
        ApiMinis.Mini[]? minis = await _apiClient.V2.Minis.GetAll(stoppingToken);
        if (skinIds is not { Length: > 0 } || minis is not { Length: > 0 })
        {
            _logger.LogWarning("Skins or minis API returned no data; skipping wardrobe update.");
            return;
        }

        Dictionary<int, Skin> existingSkins = await _dbContext.Skins.ToDictionaryAsync(s => s.Id, stoppingToken);
        int skins = 0;
        foreach (int[] page in skinIds.Chunk(PageSize))
        {
            ApiSkins.Skin[]? fetched = await _apiClient.V2.Skins.GetByIds(page, stoppingToken);
            foreach (ApiSkins.Skin skin in fetched ?? [])
            {
                if (!existingSkins.TryGetValue(skin.Id, out Skin? row))
                {
                    row = new Skin { Id = skin.Id };
                    _dbContext.Skins.Add(row);
                    existingSkins[skin.Id] = row;
                }

                Map(skin, row);
                skins++;
            }
        }

        Dictionary<int, Mini> existingMinis = await _dbContext.Minis.ToDictionaryAsync(m => m.Id, stoppingToken);
        foreach (ApiMinis.Mini mini in minis)
        {
            if (!existingMinis.TryGetValue(mini.Id, out Mini? row))
            {
                row = new Mini { Id = mini.Id };
                _dbContext.Minis.Add(row);
            }

            row.Name = mini.Name;
            row.Unlock = mini.Unlock;
            row.Icon = mini.Icon;
            row.Order = mini.Order;
            row.ItemId = mini.ItemId;
        }

        await _dbContext.SaveChangesAsync(stoppingToken);
        int unlockItems = await UpdateUnlockItemsAsync(stoppingToken);
        _logger.LogInformation(
            "Wardrobe update completed: {Skins} skins, {Minis} minis, {UnlockItems} unlock items.",
            skins, minis.Length, unlockItems);
    }

    /// <summary>The items that unlock each novelty, outfit, finisher, mail carrier, glider and jade bot.</summary>
    private async Task<int> UpdateUnlockItemsAsync(CancellationToken stoppingToken)
    {
        ApiNovelties.Novelty[]? novelties = await _apiClient.V2.Novelties.GetAll(stoppingToken);
        ApiOutfits.Outfit[]? outfits = await _apiClient.V2.Outfits.GetAll(stoppingToken);
        ApiFinishers.Finisher[]? finishers = await _apiClient.V2.Finishers.GetAll(stoppingToken);
        ApiMailCarriers.MailCarrier[]? mailCarriers = await _apiClient.V2.MailCarriers.GetAll(stoppingToken);
        ApiGliders.Glider[]? gliders = await _apiClient.V2.Gliders.GetAll(stoppingToken);
        ApiJadeBots.JadeBot[]? jadeBots = await _apiClient.V2.JadeBots.GetAll(stoppingToken);
        int rows = 0;
        rows += await StoreUnlockItemsAsync(AccountUnlockKind.Novelty,
            novelties?.Select(n => (n.Id, n.UnlockItem)), stoppingToken);
        rows += await StoreUnlockItemsAsync(AccountUnlockKind.Outfit,
            outfits?.Select(o => (o.Id, o.UnlockItems)), stoppingToken);
        rows += await StoreUnlockItemsAsync(AccountUnlockKind.Finisher,
            finishers?.Select(f => (f.Id, f.UnlockItems)), stoppingToken);
        rows += await StoreUnlockItemsAsync(AccountUnlockKind.MailCarrier,
            mailCarriers?.Select(m => (m.Id, m.UnlockItems)), stoppingToken);
        rows += await StoreUnlockItemsAsync(AccountUnlockKind.Glider,
            gliders?.Select(g => (g.Id, g.UnlockItems)), stoppingToken);
        rows += await StoreUnlockItemsAsync(AccountUnlockKind.JadeBot,
            jadeBots?.Select(j => (j.Id, new[] { j.UnlockItem })), stoppingToken);
        return rows;
    }

    // Replaces a kind's unlock items, when its catalog came back.
    private async Task<int> StoreUnlockItemsAsync(
        string kind, IEnumerable<(int UnlockId, int[] ItemIds)>? unlocks, CancellationToken stoppingToken)
    {
        if (unlocks is null)
        {
            return 0;
        }

        List<UnlockItem> rows = unlocks
            .SelectMany(u => u.ItemIds.Where(id => id > 0).Select(itemId => (u.UnlockId, ItemId: itemId)))
            .Distinct()
            .Select(u => new UnlockItem { Kind = kind, UnlockId = u.UnlockId, ItemId = u.ItemId })
            .ToList();
        await _dbContext.UnlockItems.Where(u => u.Kind == kind).ExecuteDeleteAsync(stoppingToken);
        _dbContext.UnlockItems.AddRange(rows);
        await _dbContext.SaveChangesAsync(stoppingToken);
        return rows.Count;
    }

    private static void Map(ApiSkins.Skin skin, Skin row)
    {
        // Most skins have no description; the name is always given, but stays safe should one come without it.
        row.Name = skin.Name ?? "";
        row.Type = skin.Type.Value;
        row.Flags = skin.Flags.Select(flag => flag.Value).ToArray();
        row.Restrictions = skin.Restrictions.Select(restriction => restriction.Value).ToArray();
        row.Icon = skin.Icon;
        row.Rarity = skin.Rarity.Value;
        row.Description = skin.Description ?? "";

        ApiSkins.SkinArmorDetails? armor = (skin as ApiSkins.SkinArmor)?.Details;
        row.ArmorSlot = armor?.Type.Value;
        row.WeightClass = armor?.WeightClass.Value;
        // Stored as the API spells them, with the contract's own JSON settings: {"default": [...], "overrides":
        // {"AsuraMale": [...]}}.
        row.DyeSlots = armor?.DyeSlots is { } dyeSlots
            ? JsonSerializer.Serialize(dyeSlots, Gw2ApiContractJson.Options)
            : null;

        ApiSkins.SkinWeaponDetails? weapon = (skin as ApiSkins.SkinWeapon)?.Details;
        row.WeaponType = weapon?.Type.Value;
        row.DamageType = weapon?.DamageType.Value;

        row.GatheringType = (skin as ApiSkins.SkinGathering)?.Details.Type.Value;
    }
}
