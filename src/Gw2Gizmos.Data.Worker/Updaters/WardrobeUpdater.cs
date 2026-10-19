using System.Text.Json;
using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Wardrobe;
using Gw2Gizmos.Gw2Api.Client;
using Gw2Gizmos.Gw2Api.Contract;
using Microsoft.EntityFrameworkCore;
using ApiMinis = Gw2Gizmos.Gw2Api.Contract.V2.Minis;
using ApiSkins = Gw2Gizmos.Gw2Api.Contract.V2.Skins;

namespace Gw2Gizmos.Data.Worker.Updaters;

/// <summary>
/// Full-refresh ingester for the wardrobe: every skin (<c>/v2/skins</c>, about 10,700, fetched 200 at a time) with its
/// type-specific details, and every miniature (<c>/v2/minis</c>). All public, changing only with game releases, so it
/// runs daily. Rows the API stops listing are kept.
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
        _logger.LogInformation("Wardrobe update completed: {Skins} skins, {Minis} minis.", skins, minis.Length);
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
