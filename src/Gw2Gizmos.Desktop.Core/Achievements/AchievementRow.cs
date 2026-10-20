using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Gw2Gizmos.Data.EntityFramework.Entities.Achievements;
using Gw2Gizmos.Desktop.Core.Mvvm;

namespace Gw2Gizmos.Desktop.Core;

public enum AchievementStatus
{
    NotStarted,
    InProgress,
    Done,
}

/// <summary>
/// One objective of an achievement, e.g. a collection's item, and whether the account has done it. The item it's for
/// (a mini's item too), or 0, so its name can link to the Items page.
/// </summary>
public sealed record AchievementObjective(string Text, bool Done, int ItemId = 0);

/// <summary>One reward of an achievement, e.g. "1× Mistward Legguards", and the item it is, or 0.</summary>
public sealed record AchievementRewardLine(string Text, int ItemId = 0);

/// <summary>One tier of an achievement: the count it needs, the points it gives, and whether it's reached.</summary>
public sealed record AchievementTierLine(int Number, int Count, int Points, bool Reached)
{
    /// <summary>E.g. "Tier 2 · 100 · 5 AP".</summary>
    public string Text =>
        $"Tier {Number} · {Count.ToString("N0", CultureInfo.CurrentCulture)} · {Points} AP";
}

/// <summary>
/// An achievement on the Achievements page with the account's progress: its tiers, points earned out of the total,
/// how far it is to the next tier, and its objectives and rewards. Built once per load; <see cref="IsExpanded"/> is
/// the only state that changes, kept here so a recycled list row shows the right one.
/// </summary>
public sealed class AchievementRow : ViewModelBase
{
    private bool _isExpanded;

    public AchievementRow(
        Achievement achievement,
        AccountAchievement? progress,
        AchievementCategory category,
        string groupName,
        int gameOrder,
        AchievementsData data
    )
    {
        IReadOnlyDictionary<int, AccountAchievement> allProgress = data.Progress;
        Id = achievement.Id;
        Name = achievement.Name;
        Icon = string.IsNullOrEmpty(achievement.Icon) ? category.Icon : achievement.Icon;
        CategoryName = category.Name;
        GroupName = groupName;
        GameOrder = gameOrder;
        Description = achievement.Description;
        IsRepeatable = achievement.Flags.Contains("Repeatable");
        IsHidden = AchievementsCatalog.IsHidden(achievement, progress);

        List<int> tiers = achievement.Tiers.Select(t => t.Count).ToList();
        Tiers = tiers;
        FinalCount = tiers.Count > 0 ? tiers[^1] : 0;
        bool done = progress?.Done == true;
        Current = progress?.Current ?? (done ? FinalCount : 0);
        Status =
            done ? AchievementStatus.Done
            : progress is not null && (Current > 0 || progress.Bits.Length > 0) ? AchievementStatus.InProgress
            : AchievementStatus.NotStarted;

        // The API leaves the count out of the requirement, where the two spaces are.
        Requirement = achievement.Requirement.Replace("  ", $" {Number(FinalCount)} ").Trim();

        bool needsUnlock = achievement.Flags.Contains("RequiresUnlock") && progress?.Unlocked != true;
        bool prerequisitesMissing = achievement.Prerequisites.Any(id =>
            allProgress.GetValueOrDefault(id)?.Done != true
        );
        IsLocked = !done && (needsUnlock || prerequisitesMissing);
        // The API's text for what unlocks it; failing that, its prerequisites, or a plain note.
        LockedText =
            !IsLocked ? ""
            : needsUnlock && !string.IsNullOrWhiteSpace(achievement.LockedText) ? achievement.LockedText
            : prerequisitesMissing ? "Complete its prerequisites first."
            : "Not unlocked yet.";

        // Points: each reached tier's, plus full rounds of a repeatable, up to its cap.
        int tierPoints = achievement.Tiers.Sum(t => t.Points);
        PointsTotal = achievement.PointCap is > 0 and var cap ? cap : tierPoints;
        int reached = done ? tierPoints : achievement.Tiers.Where(t => t.Count <= Current).Sum(t => t.Points);
        PointsEarned = Math.Min(PointsTotal, reached + (progress?.Repeated ?? 0) * tierPoints);

        int next = done ? -1 : tiers.FindIndex(count => count > Current);
        NextTier = next >= 0 ? next + 1 : null;
        NextTierCount = next >= 0 ? Tiers[next] : FinalCount;
        NextTierFraction = NextTierCount > 0 ? Math.Clamp((double)Current / NextTierCount, 0, 1) : 0;
        TotalFraction = done ? 1 : FinalCount > 0 ? Math.Clamp((double)Current / FinalCount, 0, 1) : 0;

        HashSet<int> bitsDone = progress?.Bits.ToHashSet() ?? [];
        Objectives = achievement.Bits
            .Select((bit, index) =>
                new AchievementObjective(ObjectiveText(bit, data), done || bitsDone.Contains(index), ItemId(bit, data))
            )
            .ToList();
        Rewards = achievement.Rewards
            .Select(reward => new AchievementRewardLine(
                RewardText(reward, data.ItemNames), reward.Type == "Item" ? reward.Id ?? 0 : 0))
            .ToList();
        TierLines = achievement
            .Tiers.Select((tier, index) =>
                new AchievementTierLine(index + 1, tier.Count, tier.Points, done || Current >= tier.Count)
            )
            .ToList();
    }

    public int Id { get; }
    public string Name { get; }
    public string Icon { get; }
    public string CategoryName { get; }
    public string GroupName { get; }

    /// <summary>The achievement's position in the game's panel: group, category, then within the category.</summary>
    public int GameOrder { get; }

    public string Description { get; }
    public string Requirement { get; }
    public AchievementStatus Status { get; }
    public bool IsDone => Status == AchievementStatus.Done;
    public bool IsRepeatable { get; }

    /// <summary>The game still hides it (not started); listed only when hidden ones are shown.</summary>
    public bool IsHidden { get; }

    /// <summary>Not yet available: it needs unlocking, or its prerequisites aren't done.</summary>
    public bool IsLocked { get; }

    public string LockedText { get; }

    /// <summary>The progress count each tier needs, in order.</summary>
    public IReadOnlyList<int> Tiers { get; }

    /// <summary>Each tier with its count, points and whether it's reached, for the details.</summary>
    public IReadOnlyList<AchievementTierLine> TierLines { get; }

    public int Current { get; }
    public int FinalCount { get; }

    /// <summary>The next tier to reach (1-based), or null when every tier is done.</summary>
    public int? NextTier { get; }

    public int NextTierCount { get; }
    public int ToNextTier => Math.Max(0, NextTierCount - Current);

    /// <summary>How far along the next tier is, 0 to 1.</summary>
    public double NextTierFraction { get; }

    /// <summary>How far along the final tier is, 0 to 1.</summary>
    public double TotalFraction { get; }

    public int PointsEarned { get; }
    public int PointsTotal { get; }
    public int PointsLeft => PointsTotal - PointsEarned;

    /// <summary>Whether it's worth a look for a quick win: unfinished, available, with a tier to reach.</summary>
    public bool IsQuickWinCandidate => !IsDone && !IsLocked && !IsRepeatable && NextTier is not null;

    /// <summary>Whether the row is open, showing its details.</summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);
    public bool HasRequirement => Requirement.Length > 0;

    public IReadOnlyList<AchievementObjective> Objectives { get; }
    public IReadOnlyList<AchievementRewardLine> Rewards { get; }
    public bool HasObjectives => Objectives.Count > 0;
    public bool HasRewards => Rewards.Count > 0;

    /// <summary>E.g. "120 / 500 · tier 3 of 4", "Done" or "Locked".</summary>
    public string ProgressText =>
        IsDone ? "Done"
        : IsLocked ? "Locked"
        : Tiers.Count > 1 && NextTier is { } tier
            ? $"{Number(Current)} / {Number(NextTierCount)} · tier {tier} of {Tiers.Count}"
        : $"{Number(Current)} / {Number(NextTierCount)}";

    /// <summary>E.g. "5 / 15 AP".</summary>
    public string PointsText => $"{PointsEarned} / {PointsTotal} AP";

    /// <summary>Where each tier ends along the bar, 0 to 1 (the last is the bar's end, so it's left out).</summary>
    public IReadOnlyList<double> TierMarks =>
        FinalCount > 0 ? Tiers.Take(Tiers.Count - 1).Select(count => (double)count / FinalCount).ToList() : [];

    /// <summary>
    /// A text objective's text, or the name of the item, skin or mini it refers to (their text is a hint on where to
    /// get it, e.g. "Dropped by Sons of Svanir enemies.", so it doesn't stand in for the name).
    /// </summary>
    private static string ObjectiveText(AchievementBit bit, AchievementsData data)
    {
        IReadOnlyDictionary<int, string>? names = bit.Type switch
        {
            "Item" => data.ItemNames,
            "Skin" => data.SkinNames,
            "Minipet" => data.MiniNames,
            _ => null,
        };
        return
            names is not null && names.TryGetValue(bit.Id, out string? name) ? name
            : bit.Type == "Text" && !string.IsNullOrWhiteSpace(bit.Text) ? bit.Text
            : $"{bit.Type} {bit.Id}";
    }

    /// <summary>The item an objective is for: an item's own id, or a mini's item; 0 for others.</summary>
    private static int ItemId(AchievementBit bit, AchievementsData data) =>
        bit.Type switch
        {
            "Item" => bit.Id,
            "Minipet" => data.MiniItemIds.GetValueOrDefault(bit.Id),
            _ => 0,
        };

    private static string RewardText(AchievementReward reward, IReadOnlyDictionary<int, string> itemNames) =>
        reward.Type switch
        {
            "Item" when reward.Id is { } id =>
                $"{reward.Count ?? 1}× {itemNames.GetValueOrDefault(id) ?? $"item {id}"}",
            "Coins" => Coin.Format(reward.Count ?? 0),
            "Mastery" => reward.Region is { Length: > 0 } region ? $"Mastery point ({region})" : "Mastery point",
            "Title" => "Title",
            _ => reward.Type,
        };

    private static string Number(int value) => value.ToString("N0", CultureInfo.CurrentCulture);
}
