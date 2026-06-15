using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Accounts;

/// <summary>
/// A character's core details for an account (the <c>Characters</c> table), upserted each sync from the full
/// character object. This is the source of truth for the character list — the selector reads names from here, not
/// from the bag snapshot. Race/Gender/Profession are stored as their API enum names for direct display. Guild id
/// and title are nullable; the build/equipment tab counts need the key's <c>builds</c> permission (otherwise 0).
/// </summary>
[PrimaryKey(nameof(AccountId), nameof(Name))]
public class Character
{
    [MaxLength(64)]
    public string AccountId { get; set; } = "";

    [MaxLength(64)]
    public string Name { get; set; } = "";

    [MaxLength(16)]
    public string Race { get; set; } = "";

    [MaxLength(16)]
    public string Gender { get; set; } = "";

    [MaxLength(32)]
    public string Profession { get; set; } = "";

    public int Level { get; set; }

    /// <summary>The character's represented guild id, or <c>null</c> when none.</summary>
    [MaxLength(64)]
    public string? Guild { get; set; }

    /// <summary>Total played time in seconds.</summary>
    public int Age { get; set; }

    public DateTimeOffset Created { get; set; }

    public DateTimeOffset LastModified { get; set; }

    public int Deaths { get; set; }

    /// <summary>The displayed title id, or <c>null</c> when none.</summary>
    public int? Title { get; set; }

    public int BuildTabsUnlocked { get; set; }

    public int ActiveBuildTab { get; set; }

    public int EquipmentTabsUnlocked { get; set; }

    public int ActiveEquipmentTab { get; set; }
}
