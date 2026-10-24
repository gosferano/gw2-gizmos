using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Accounts;

/// <summary>
/// A legendary in the account's legendary armory (<c>/v2/account/legendaryarmory</c>), and how many of it. Replaced on
/// every sync.
/// </summary>
[Table("AccountLegendaryArmory")]
[PrimaryKey(nameof(AccountId), nameof(ItemId))]
public class AccountLegendaryItem
{
    [MaxLength(64)]
    public string AccountId { get; set; } = "";

    public int ItemId { get; set; }

    public int Count { get; set; }
}
