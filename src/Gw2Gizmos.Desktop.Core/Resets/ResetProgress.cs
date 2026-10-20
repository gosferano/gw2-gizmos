using System;
using System.Collections.Generic;
using System.Linq;
using Gw2Gizmos.Data.EntityFramework.Entities.Resets;
using Gw2Gizmos.Data.Static;
using Gw2Gizmos.Data.Static.Events;
using Gw2Gizmos.Data.Static.Resets;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>
/// A once-per-reset activity as the page shows it: its name, the item it is (a daily craft) or 0, the event on the
/// Events page's schedule it is (a world boss or map meta) when known, and whether it's done this reset.
/// </summary>
public sealed record ResetActivity(string Id, string Name, int ItemId, ScheduledEvent? Event, bool Done)
{
    /// <summary>Not on a timer: a map meta that runs whenever players start it.</summary>
    public bool AnyTime { get; init; }
}

/// <summary>A raid wing and its bosses this week (checkpoints aren't clears, so they're left out).</summary>
public sealed record RaidWing(string Name, IReadOnlyList<ResetActivity> Bosses)
{
    public int Cleared => Bosses.Count(b => b.Done);

    public bool Done => Bosses.Count > 0 && Cleared == Bosses.Count;
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
                Event(a),
                a.Done && a.SyncedAtUtc >= since)
            {
                AnyTime = kind == ResetActivityKind.MapChest && ResetActivities.UntimedMapChests.Contains(a.ActivityId),
            })
            .ToList();
    }

    /// <summary>The raid wings and their bosses cleared this week, in the API's order.</summary>
    public static IReadOnlyList<RaidWing> Raids(ResetProgressData data, DateTimeOffset now)
    {
        DateTimeOffset since = GameResets.LastWeekly(now);
        return data.Activities
            .Where(a => a.Kind == ResetActivityKind.Raid && !a.IsCheckpoint && a.Group is not null)
            .GroupBy(a => a.Group!)
            .Select(wing => new RaidWing(
                ResetActivities.RaidName(wing.Key),
                wing.Select(a => new ResetActivity(
                        a.ActivityId,
                        ResetActivities.RaidName(a.ActivityId),
                        0,
                        null,
                        a.Done && a.SyncedAtUtc >= since))
                    .ToList()))
            .ToList();
    }

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
            ResetActivityKind.WorldBoss when Event(activity) is { } boss => boss.Name,
            ResetActivityKind.MapChest
                when ResetActivities.MapChestMaps.TryGetValue(activity.ActivityId, out string? map) => map,
            ResetActivityKind.DailyCrafting
                when ResetActivities.DailyCraftingItems.TryGetValue(activity.ActivityId, out int itemId)
                && itemNames.TryGetValue(itemId, out string? item) => item,
            _ => ResetActivities.NameFromId(activity.ActivityId),
        };

    private static ScheduledEvent? Event(AccountResetActivity activity)
    {
        IReadOnlyDictionary<string, string>? events = activity.Kind switch
        {
            ResetActivityKind.WorldBoss => ResetActivities.WorldBossEvents,
            ResetActivityKind.MapChest => ResetActivities.MapChestEvents,
            _ => null,
        };
        return events is not null && events.TryGetValue(activity.ActivityId, out string? eventId)
            ? EventSchedule.All.FirstOrDefault(e => e.Id == eventId)
            : null;
    }
}
