using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Wardrobe;

/// <summary>
/// A dye, mini or skin the account has unlocked (<c>/v2/account/dyes</c>, <c>minis</c> and <c>skins</c>). Each kind's
/// rows are replaced on every sync.
/// </summary>
[Table("AccountUnlocks")]
[PrimaryKey(nameof(AccountId), nameof(Kind), nameof(UnlockId))]
public class AccountUnlock
{
    [MaxLength(64)]
    public string AccountId { get; set; } = "";

    /// <summary>One of <see cref="AccountUnlockKind"/>.</summary>
    [MaxLength(16)]
    public string Kind { get; set; } = "";

    /// <summary>The dye's colour id, the mini's id or the skin's id.</summary>
    public int UnlockId { get; set; }
}

/// <summary>The kinds of <see cref="AccountUnlock"/>.</summary>
public static class AccountUnlockKind
{
    public const string Dye = "Dye";
    public const string Mini = "Mini";
    public const string Skin = "Skin";
}
