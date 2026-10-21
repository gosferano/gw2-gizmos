using System;
using System.Collections.Generic;
using System.Linq;
using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Achievements;
using Gw2Gizmos.Data.Static.Achievements;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// Everything the Achievements page shows: the game's groups and categories, every achievement, the account's
/// progress, and the names of the items, skins and minis achievements list.
/// </summary>
public sealed record AchievementsData(
    IReadOnlyList<AchievementGroup> Groups,
    IReadOnlyDictionary<int, AchievementCategory> Categories,
    IReadOnlyDictionary<int, Achievement> Achievements,
    IReadOnlyDictionary<int, AccountAchievement> Progress,
    IReadOnlyDictionary<int, string> ItemNames
)
{
    public IReadOnlyDictionary<int, string> SkinNames { get; init; } = new Dictionary<int, string>();
    public IReadOnlyDictionary<int, string> MiniNames { get; init; } = new Dictionary<int, string>();

    /// <summary>The item each mini comes from, by mini id.</summary>
    public IReadOnlyDictionary<int, int> MiniItemIds { get; init; } = new Dictionary<int, int>();

    public static AchievementsData Empty { get; } =
        new([], new Dictionary<int, AchievementCategory>(), new Dictionary<int, Achievement>(),
            new Dictionary<int, AccountAchievement>(), new Dictionary<int, string>());
}

/// <summary>
/// Read-only access to the worker-synced achievements for the Achievements page. Opens its own short scope per call;
/// meant to run off the UI thread.
/// </summary>
public sealed class AchievementsReader
{
    private readonly IServiceScopeFactory _scopeFactory;

    public AchievementsReader(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    /// <summary>The achievements and, given an account, its progress; empty when nothing has synced yet.</summary>
    public AchievementsData Read(string? accountId)
    {
        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            if (!DatabaseSchema.Allows(scope))
            {
                return AchievementsData.Empty;
            }

            var db = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();

            List<AchievementGroup> groups = db.AchievementGroups.AsNoTracking().OrderBy(g => g.Order).ToList();
            Dictionary<int, AchievementCategory> categories =
                db.AchievementCategories.AsNoTracking().ToDictionary(c => c.Id);
            Dictionary<int, Achievement> achievements = db.Achievements.AsNoTracking().ToDictionary(a => a.Id);
            Dictionary<int, AccountAchievement> progress = accountId is null
                ? new Dictionary<int, AccountAchievement>()
                : db.AccountAchievements.AsNoTracking()
                    .Where(a => a.AccountId == accountId)
                    .ToDictionary(a => a.AchievementId);

            // Names of the items objectives and rewards refer to (skins and minipets aren't stored), and for the items
            // the API doesn't list, the names shipped with the app.
            HashSet<int> itemIds = achievements.Values
                .SelectMany(a => a.Bits.Where(b => b.Type == "Item").Select(b => b.Id)
                    .Concat(a.Rewards.Where(r => r.Type == "Item" && r.Id is not null).Select(r => r.Id!.Value)))
                .ToHashSet();
            Dictionary<int, string> itemNames = db.Items.AsNoTracking()
                .Where(item => itemIds.Contains(item.Id))
                .Select(item => new { item.Id, item.Name })
                .ToDictionary(item => item.Id, item => item.Name);
            foreach (int id in itemIds.Where(id => !itemNames.ContainsKey(id)))
            {
                if (UnlistedItems.NamesById.TryGetValue(id, out string? name))
                {
                    itemNames[id] = name;
                }
            }

            // Names of the skins and minis objectives refer to (e.g. a collection's armor pieces).
            HashSet<int> skinIds = BitIds(achievements.Values, "Skin");
            HashSet<int> miniIds = BitIds(achievements.Values, "Minipet");
            Dictionary<int, string> skinNames = db.Skins.AsNoTracking()
                .Where(skin => skinIds.Contains(skin.Id))
                .ToDictionary(skin => skin.Id, skin => skin.Name);
            var minis = db.Minis.AsNoTracking()
                .Where(mini => miniIds.Contains(mini.Id))
                .Select(mini => new { mini.Id, mini.Name, mini.ItemId })
                .ToList();

            return new AchievementsData(groups, categories, achievements, progress, itemNames)
            {
                SkinNames = skinNames,
                MiniNames = minis.ToDictionary(mini => mini.Id, mini => mini.Name),
                MiniItemIds = minis.ToDictionary(mini => mini.Id, mini => mini.ItemId),
            };
        }
        catch (Exception)
        {
            return AchievementsData.Empty;
        }
    }

    private static HashSet<int> BitIds(IEnumerable<Achievement> achievements, string type) =>
        achievements.SelectMany(a => a.Bits.Where(b => b.Type == type).Select(b => b.Id)).ToHashSet();
}
