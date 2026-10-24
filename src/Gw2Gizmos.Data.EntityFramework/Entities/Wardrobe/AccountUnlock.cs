using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Wardrobe;

/// <summary>
/// A dye, mini, skin, novelty, outfit, finisher, mail carrier, glider or jade bot the account has unlocked
/// (<c>/v2/account/dyes</c>, <c>minis</c>, <c>skins</c>, …). Each kind's rows are replaced on every sync.
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

    /// <summary>The unlock's id in its kind's API: the dye's colour id, the mini's id, the skin's id, …</summary>
    public int UnlockId { get; set; }
}

/// <summary>The kinds of <see cref="AccountUnlock"/>.</summary>
public static class AccountUnlockKind
{
    public const string Dye = "Dye";
    public const string Mini = "Mini";
    public const string Skin = "Skin";
    public const string Novelty = "Novelty";
    public const string Outfit = "Outfit";
    public const string Finisher = "Finisher";
    public const string MailCarrier = "MailCarrier";
    public const string Glider = "Glider";
    public const string JadeBot = "JadeBot";
}
