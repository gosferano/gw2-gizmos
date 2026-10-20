using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Resets;

/// <summary>
/// One activity the account can do once per reset — a world boss, a map's meta reward chest, a daily craft, a raid
/// encounter or a dungeon path — and whether it's done. Every activity the API lists is stored (its master list, e.g.
/// <c>/v2/worldbosses</c>), marked done when the account's list (e.g. <c>/v2/account/worldbosses</c>) has it. An
/// account's rows of a kind are replaced on every sync.
/// </summary>
[Table("AccountResetActivities")]
[PrimaryKey(nameof(AccountId), nameof(Kind), nameof(ActivityId))]
public class AccountResetActivity
{
    [MaxLength(64)]
    public string AccountId { get; set; } = "";

    /// <summary>One of <see cref="ResetActivityKind"/>.</summary>
    [MaxLength(16)]
    public string Kind { get; set; } = "";

    /// <summary>The API's id, e.g. "tequatl_the_sunless".</summary>
    [MaxLength(64)]
    public string ActivityId { get; set; } = "";

    /// <summary>What it belongs to: a raid encounter's wing or a dungeon path's dungeon; null for the others.</summary>
    [MaxLength(64)]
    public string? Group { get; set; }

    /// <summary>The position in the API's list (raid encounters in wing order).</summary>
    public int Order { get; set; }

    /// <summary>A raid encounter that's a checkpoint rather than a boss; false for the other kinds.</summary>
    public bool IsCheckpoint { get; set; }

    public bool Done { get; set; }

    public DateTimeOffset SyncedAtUtc { get; set; }
}

/// <summary>The kinds of <see cref="AccountResetActivity"/>, and the reset each one follows.</summary>
public static class ResetActivityKind
{
    /// <summary>World bosses killed today (daily reset).</summary>
    public const string WorldBoss = "WorldBoss";

    /// <summary>Map meta reward chests opened today (daily reset).</summary>
    public const string MapChest = "MapChest";

    /// <summary>Time-gated crafts made today (daily reset).</summary>
    public const string DailyCrafting = "DailyCrafting";

    /// <summary>Raid encounters cleared this week (weekly reset).</summary>
    public const string Raid = "Raid";

    /// <summary>Dungeon paths completed today (daily reset).</summary>
    public const string Dungeon = "Dungeon";
}
