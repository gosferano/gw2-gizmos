using System;
using System.Collections.Generic;
using System.Linq;
using Gw2Gizmos.Data.EntityFramework.Entities.Achievements;
using Gw2Gizmos.Data.EntityFramework.Entities.Resets;
using Gw2Gizmos.Data.Static;
using Gw2Gizmos.Data.Static.Events;
using Gw2Gizmos.Data.Static.Resets;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// A once-per-reset activity as the page shows it: its name, the item it is (a daily craft) or 0, the events on the
/// Events page's schedule it is (a world boss or map meta; several for the Convergences' shared chest), and whether
/// it's done this reset.
/// </summary>
public sealed record ResetActivity(
    string Id, string Name, int ItemId, IReadOnlyList<ScheduledEvent> Events, bool Done)
{
    /// <summary>Its event that's on or starts next, and when; null without a scheduled event.</summary>
    public (ScheduledEvent Event, EventTiming Timing)? Next(DateTimeOffset now) =>
        Events.Count == 0
            ? null
            : Events.Select(e => (e, EventTiming.Of(e, now))).MinBy(pair => pair.Item2.SortKey);

    /// <summary>Not on a timer: a map meta that runs whenever players start it.</summary>
    public bool AnyTime { get; init; }
}

/// <summary>
/// Activities done together in one place: a raid wing and its bosses this week (checkpoints aren't clears, so they're
/// left out), or a dungeon and its paths today.
/// </summary>
public sealed record ResetActivityGroup(string Name, IReadOnlyList<ResetActivity> Activities)
{
    public int Cleared => Activities.Count(a => a.Done);

    public bool Done => Activities.Count > 0 && Cleared == Activities.Count;
}

/// <summary>An achievement of a rotating category this reset, and the account's progress on it.</summary>
public sealed record RotatingAchievement(int Id, string Name, bool Done, int Current, int Max);

/// <summary>A rotating achievement category (e.g. Daily Fractals), its icon, and its achievements this reset.</summary>
public sealed record RotatingCategory(string Name, string Icon, IReadOnlyList<RotatingAchievement> Achievements)
{
    public int Done => Achievements.Count(a => a.Done);
}

/// <summary>A Wizard's Vault objective and its progress.</summary>
public sealed record WizardsVaultObjectiveLine(
    string Title, string Track, int Acclaim, int Current, int Complete, bool Claimed)
{
    /// <summary>Done but its Astral Acclaim not yet claimed in game.</summary>
    public bool ReadyToClaim => !Claimed && Complete > 0 && Current >= Complete;
}

/// <summary>
/// One of the Wizard's Vault's objective sets (daily, weekly or special) and its meta reward (none for the special
/// one). <see cref="IsStale"/> when it was synced before the reset that replaced it: its objectives are gone then.
/// </summary>
public sealed record WizardsVaultSet(
    string Period,
    bool IsStale,
    IReadOnlyList<WizardsVaultObjectiveLine> Objectives,
    AccountWizardsVaultMeta? Meta
)
{
    public int Done => Objectives.Count(o => o.Claimed || o.ReadyToClaim);

    /// <summary>The Astral Acclaim not yet claimed: unclaimed objectives and an unclaimed meta reward.</summary>
    public int AcclaimLeft =>
        IsStale ? 0
        : Objectives.Where(o => !o.Claimed).Sum(o => o.Acclaim) + (Meta is { RewardClaimed: false } meta
            ? meta.RewardAstral
            : 0);
}

/// <summary>
/// Turns the stored reset progress into what the Daily and weekly page shows. Progress synced before the reset it
/// belongs to is from the previous reset: the activities read as not done (the reset cleared them), and the
/// Wizard's Vault's set reads as stale until the next sync brings the new objectives.
/// </summary>
public static class ResetProgress
{
    /// <summary>An account's activities of one daily kind (world bosses, map chests, daily crafting), in the API's
    /// order.</summary>
    public static IReadOnlyList<ResetActivity> Activities(ResetProgressData data, string kind, DateTimeOffset now)
    {
        DateTimeOffset since = GameResets.LastDaily(now);
        return data.Activities
            .Where(a => a.Kind == kind)
            .Select(a => new ResetActivity(
                a.ActivityId,
                Name(a, data.ItemNames),
                kind == ResetActivityKind.DailyCrafting
                    ? ResetActivities.DailyCraftingItems.GetValueOrDefault(a.ActivityId)
                    : 0,
                Events(a),
                a.Done && a.SyncedAtUtc >= since)
            {
                AnyTime = kind == ResetActivityKind.MapChest && ResetActivities.UntimedMapChests.Contains(a.ActivityId),
            })
            .ToList();
    }

    /// <summary>The raid wings and their bosses cleared this week, in the API's order.</summary>
    public static IReadOnlyList<ResetActivityGroup> Raids(ResetProgressData data, DateTimeOffset now) =>
        Groups(
            data.Activities.Where(a => a.Kind == ResetActivityKind.Raid && !a.IsCheckpoint),
            GameResets.LastWeekly(now),
            ResetActivities.RaidName,
            ResetActivities.RaidName);

    /// <summary>The dungeons and their paths completed today, in the API's order.</summary>
    public static IReadOnlyList<ResetActivityGroup> Dungeons(ResetProgressData data, DateTimeOffset now) =>
        Groups(
            data.Activities.Where(a => a.Kind == ResetActivityKind.Dungeon),
            GameResets.LastDaily(now),
            ResetActivities.DungeonName,
            ResetActivities.DungeonPathName);

    /// <summary>
    /// The rotating achievement categories to show, in the page's order: the ones always shown, and a festival's
    /// dailies while <paramref name="festival"/> is it. Progress synced before the achievement's reset (weekly for
    /// one flagged weekly, otherwise daily) is from the previous reset, so it reads as not started.
    /// </summary>
    public static IReadOnlyList<RotatingCategory> RotatingAchievements(
        ResetProgressData data, Festival? festival, DateTimeOffset now)
    {
        DateTimeOffset daily = GameResets.LastDaily(now);
        DateTimeOffset weekly = GameResets.LastWeekly(now);
        return data.RotatingCategories
            .Where(c => !ResetActivities.FestivalCategories.TryGetValue(c.Id, out Festival of) || of == festival)
            .Select(c => new RotatingCategory(
                ResetActivities.RotatingCategoryNames.GetValueOrDefault(c.Id) ?? c.Name,
                c.Icon,
                c.AchievementIds
                    .Where(data.Achievements.ContainsKey)
                    .Select(id => Rotating(data.Achievements[id], data.AchievementProgress.GetValueOrDefault(id),
                        data.Achievements[id].Flags.Contains("Weekly") ? weekly : daily))
                    .ToList()))
            .Where(c => c.Achievements.Count > 0)
            .ToList();
    }

    private static RotatingAchievement Rotating(
        Achievement achievement, AccountAchievement? progress, DateTimeOffset since)
    {
        AccountAchievement? current = progress is not null && progress.SyncedAtUtc >= since ? progress : null;
        int max = current?.Max ?? achievement.Tiers.LastOrDefault()?.Count ?? 0;
        return new RotatingAchievement(
            achievement.Id, achievement.Name, current?.Done == true, current?.Current ?? 0, max);
    }

    private static IReadOnlyList<ResetActivityGroup> Groups(
        IEnumerable<AccountResetActivity> activities, DateTimeOffset since, Func<string, string> groupName,
        Func<string, string> activityName) =>
        activities
            .Where(a => a.Group is not null)
            .GroupBy(a => a.Group!)
            .Select(group => new ResetActivityGroup(
                groupName(group.Key),
                group.Select(a => new ResetActivity(
                        a.ActivityId, activityName(a.ActivityId), 0, [], a.Done && a.SyncedAtUtc >= since))
                    .ToList()))
            .ToList();

    /// <summary>The Wizard's Vault's set for a period; null when it hasn't synced.</summary>
    public static WizardsVaultSet? WizardsVault(ResetProgressData data, string period, DateTimeOffset now)
    {
        List<AccountWizardsVaultObjective> objectives = data.Objectives.Where(o => o.Period == period).ToList();
        AccountWizardsVaultMeta? meta = data.Metas.FirstOrDefault(m => m.Period == period);
        if (objectives.Count == 0 && meta is null)
        {
            return null;
        }

        // The special objectives last the season, not a reset.
        DateTimeOffset? since = period switch
        {
            WizardsVaultPeriod.Daily => GameResets.LastDaily(now),
            WizardsVaultPeriod.Weekly => GameResets.LastWeekly(now),
            _ => null,
        };
        DateTimeOffset synced = meta?.SyncedAtUtc ?? objectives.Min(o => o.SyncedAtUtc);
        return new WizardsVaultSet(
            period,
            since is { } reset && synced < reset,
            objectives.Select(o => new WizardsVaultObjectiveLine(
                    o.Title, o.Track, o.Acclaim, o.ProgressCurrent, o.ProgressComplete, o.Claimed))
                .ToList(),
            meta);
    }

    private static string Name(AccountResetActivity activity, IReadOnlyDictionary<int, string> itemNames) =>
        activity.Kind switch
        {
            ResetActivityKind.WorldBoss when Events(activity) is [var boss] => boss.Name,
            ResetActivityKind.MapChest
                when ResetActivities.MapChestMaps.TryGetValue(activity.ActivityId, out string? map) => map,
            ResetActivityKind.DailyCrafting
                when ResetActivities.DailyCraftingItems.TryGetValue(activity.ActivityId, out int itemId)
                && itemNames.TryGetValue(itemId, out string? item) => item,
            _ => ResetActivities.NameFromId(activity.ActivityId),
        };

    private static IReadOnlyList<ScheduledEvent> Events(AccountResetActivity activity)
    {
        IReadOnlyList<string> eventIds = activity.Kind switch
        {
            ResetActivityKind.WorldBoss when ResetActivities.WorldBossEvents.TryGetValue(activity.ActivityId,
                out string? boss) => [boss],
            ResetActivityKind.MapChest when ResetActivities.MapChestEvents.TryGetValue(activity.ActivityId,
                out IReadOnlyList<string>? metas) => metas,
            _ => [],
        };
        return EventSchedule.All.Where(e => eventIds.Contains(e.Id)).ToList();
    }
}
