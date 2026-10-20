using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Resets;

/// <summary>
/// One of the account's current Wizard's Vault objectives (<c>/v2/account/wizardsvault/{daily,weekly,special}</c>)
/// with its progress. An account's rows of a period are replaced on every sync.
/// </summary>
[Table("AccountWizardsVaultObjectives")]
[PrimaryKey(nameof(AccountId), nameof(Period), nameof(ObjectiveId))]
public class AccountWizardsVaultObjective
{
    [MaxLength(64)]
    public string AccountId { get; set; } = "";

    /// <summary>One of <see cref="WizardsVaultPeriod"/>.</summary>
    [MaxLength(8)]
    public string Period { get; set; } = "";

    public int ObjectiveId { get; set; }

    /// <summary>The objective as the game words it, e.g. "Complete 3 Events".</summary>
    public string Title { get; set; } = "";

    /// <summary>"PvE", "PvP" or "WvW".</summary>
    [MaxLength(8)]
    public string Track { get; set; } = "";

    /// <summary>The Astral Acclaim it gives.</summary>
    public int Acclaim { get; set; }

    public int ProgressCurrent { get; set; }

    public int ProgressComplete { get; set; }

    /// <summary>Whether its Astral Acclaim has been claimed.</summary>
    public bool Claimed { get; set; }

    public DateTimeOffset SyncedAtUtc { get; set; }
}

/// <summary>
/// The meta reward of a Wizard's Vault period (daily or weekly; the special objectives have none): how many of its
/// objectives are done toward it, and its reward. An account's row of a period is replaced on every sync.
/// </summary>
[Table("AccountWizardsVaultMetas")]
[PrimaryKey(nameof(AccountId), nameof(Period))]
public class AccountWizardsVaultMeta
{
    [MaxLength(64)]
    public string AccountId { get; set; } = "";

    /// <summary><see cref="WizardsVaultPeriod.Daily"/> or <see cref="WizardsVaultPeriod.Weekly"/>.</summary>
    [MaxLength(8)]
    public string Period { get; set; } = "";

    public int ProgressCurrent { get; set; }

    public int ProgressComplete { get; set; }

    /// <summary>The item the meta reward gives.</summary>
    public int RewardItemId { get; set; }

    /// <summary>The Astral Acclaim the meta reward gives.</summary>
    public int RewardAstral { get; set; }

    public bool RewardClaimed { get; set; }

    public DateTimeOffset SyncedAtUtc { get; set; }
}

/// <summary>The Wizard's Vault's objective sets.</summary>
public static class WizardsVaultPeriod
{
    public const string Daily = "Daily";
    public const string Weekly = "Weekly";

    /// <summary>The special (limited-time) objectives, which last the season rather than a reset.</summary>
    public const string Special = "Special";
}
