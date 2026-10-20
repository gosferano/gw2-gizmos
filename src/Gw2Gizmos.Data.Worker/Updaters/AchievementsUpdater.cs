using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Achievements;
using Gw2Gizmos.Gw2Api.Client;
using Microsoft.EntityFrameworkCore;
using Api = Gw2Gizmos.Gw2Api.Contract.V2.Achievements;

namespace Gw2Gizmos.Data.Worker.Updaters;

/// <summary>
/// Full-refresh ingester for achievements: the groups (<c>/v2/achievements/groups</c>) and categories
/// (<c>/v2/achievements/categories</c>) of the game's achievement panel, and every achievement
/// (<c>/v2/achievements</c>, about 8,000, fetched 200 at a time). All public, changing only with game releases, so it
/// runs daily. Rows the API stops listing are kept.
/// </summary>
public class AchievementsUpdater
{
    private const int PageSize = 200;

    private readonly Gw2GizmosDbContext _dbContext;
    private readonly Gw2ApiClient _apiClient;
    private readonly ILogger<AchievementsUpdater> _logger;

    public AchievementsUpdater(
        Gw2GizmosDbContext dbContext,
        IGw2ApiClientFactory apiClientFactory,
        ILogger<AchievementsUpdater> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
        _apiClient = apiClientFactory.Create(Locale.English);
    }

    public async Task UpdateAchievements(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting achievements update...");

        Api.AchievementGroup[]? groups = await _apiClient.V2.Achievements.Groups.GetAll(stoppingToken);
        Api.AchievementCategory[]? categories = await _apiClient.V2.Achievements.Categories.GetAll(stoppingToken);
        int[]? achievementIds = await _apiClient.V2.Achievements.GetIds(stoppingToken);
        if (groups is not { Length: > 0 } || categories is not { Length: > 0 } || achievementIds is not { Length: > 0 })
        {
            _logger.LogWarning("Achievements API returned no data; skipping achievements update.");
            return;
        }

        Dictionary<string, AchievementGroup> existingGroups =
            await _dbContext.AchievementGroups.ToDictionaryAsync(g => g.Id, stoppingToken);
        foreach (Api.AchievementGroup group in groups)
        {
            if (!existingGroups.TryGetValue(group.Id, out AchievementGroup? row))
            {
                row = new AchievementGroup { Id = group.Id };
                _dbContext.AchievementGroups.Add(row);
            }

            row.Name = group.Name;
            row.Description = group.Description;
            row.Order = group.Order;
            row.CategoryIds = group.Categories;
        }

        Dictionary<int, AchievementCategory> existingCategories =
            await _dbContext.AchievementCategories.ToDictionaryAsync(c => c.Id, stoppingToken);
        foreach (Api.AchievementCategory category in categories)
        {
            if (!existingCategories.TryGetValue(category.Id, out AchievementCategory? row))
            {
                row = new AchievementCategory { Id = category.Id };
                _dbContext.AchievementCategories.Add(row);
            }

            row.Name = category.Name;
            row.Description = category.Description;
            row.Order = category.Order;
            row.Icon = category.Icon;
            row.AchievementIds = category.Achievements.Select(a => a.Id).ToArray();
            row.TomorrowAchievementIds = category.Tomorrow.Select(a => a.Id).ToArray();
        }

        await _dbContext.SaveChangesAsync(stoppingToken);

        Dictionary<int, Achievement> existingAchievements =
            await _dbContext.Achievements.ToDictionaryAsync(a => a.Id, stoppingToken);
        int stored = 0;
        foreach (int[] page in achievementIds.Chunk(PageSize))
        {
            Api.Achievement[]? achievements = await _apiClient.V2.Achievements.GetByIds(page, stoppingToken);
            foreach (Api.Achievement achievement in achievements ?? [])
            {
                if (!existingAchievements.TryGetValue(achievement.Id, out Achievement? row))
                {
                    row = new Achievement { Id = achievement.Id };
                    _dbContext.Achievements.Add(row);
                    existingAchievements[achievement.Id] = row;
                }

                Map(achievement, row);
                stored++;
            }
        }

        await _dbContext.SaveChangesAsync(stoppingToken);
        _logger.LogInformation(
            "Achievements update completed: {Groups} groups, {Categories} categories, {Achievements} achievements.",
            groups.Length,
            categories.Length,
            stored
        );
    }

    private static void Map(Api.Achievement achievement, Achievement row)
    {
        row.Name = achievement.Name;
        row.Icon = achievement.Icon;
        row.Description = achievement.Description;
        row.Requirement = achievement.Requirement;
        row.LockedText = achievement.LockedText;
        row.Type = achievement.Type.Value;
        row.Flags = achievement.Flags.Select(flag => flag.Value).ToArray();
        row.Prerequisites = achievement.Prerequisites;
        row.PointCap = achievement.PointCap;
        row.Tiers = achievement
            .Tiers.Select(tier => new AchievementTier { Count = tier.Count, Points = tier.Points })
            .ToList();
        row.Bits = achievement
            .Bits.Select(bit => new AchievementBit { Type = bit.Type.Value, Id = bit.Id, Text = bit.Text })
            .ToList();
        row.Rewards = achievement
            .Rewards.Select(reward => new AchievementReward
            {
                Type = reward.Type.Value,
                Id = reward.Id,
                Count = reward.Count,
                Region = reward.Region,
            })
            .ToList();
    }
}
