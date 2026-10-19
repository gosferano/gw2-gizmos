using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Achievements;

/// <summary>
/// An account's progress on one achievement (<c>/v2/account/achievements</c>). The API lists only achievements with
/// some progress, so one missing here isn't started. An account's rows are replaced on every sync.
/// </summary>
[Table("AccountAchievements")]
[PrimaryKey(nameof(AccountId), nameof(AchievementId))]
public class AccountAchievement
{
    [MaxLength(64)]
    public string AccountId { get; set; } = "";

    public int AchievementId { get; set; }

    /// <summary>The progress count toward the tiers; null for an achievement without a count.</summary>
    public int? Current { get; set; }

    /// <summary>The count the final tier needs.</summary>
    public int? Max { get; set; }

    public bool Done { get; set; }

    /// <summary>The indexes of the objectives (bits) done (stored as a JSON array).</summary>
    public int[] Bits { get; set; } = [];

    /// <summary>How many times a repeatable achievement has been completed.</summary>
    public int? Repeated { get; set; }

    /// <summary>For an achievement that needs unlocking, whether it's unlocked; null when it doesn't.</summary>
    public bool? Unlocked { get; set; }

    public DateTimeOffset SyncedAtUtc { get; set; }
}
