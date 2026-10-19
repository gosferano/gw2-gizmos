using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Achievements;

/// <summary>
/// An achievement from <c>/v2/achievements</c> (master data, refreshed daily): its tiers and the points each gives, its
/// objectives (bits) and its rewards. Tiers, bits and rewards are a few entries each, stored as JSON columns.
/// </summary>
[Table("Achievements")]
public class Achievement
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    [MaxLength(256)]
    public string Name { get; set; } = "";

    [MaxLength(256)]
    public string? Icon { get; set; }

    public string Description { get; set; } = "";

    /// <summary>What completing it takes, e.g. "Kill  centaurs." (the API leaves the count out, where the two spaces
    /// are).</summary>
    public string Requirement { get; set; } = "";

    /// <summary>The text shown while it's locked.</summary>
    public string LockedText { get; set; } = "";

    /// <summary>"Default", or "ItemSet" for a collection.</summary>
    [MaxLength(16)]
    public string Type { get; set; } = "";

    /// <summary>The API's flags, e.g. "Repeatable", "Hidden", "Daily" (stored as a JSON array).</summary>
    public string[] Flags { get; set; } = [];

    /// <summary>Achievements to complete before this one unlocks (stored as a JSON array).</summary>
    public int[] Prerequisites { get; set; } = [];

    /// <summary>For a repeatable achievement, the most points it can give in total.</summary>
    public int? PointCap { get; set; }

    /// <summary>The tiers, in order: the progress count each needs and the points it gives.</summary>
    public List<AchievementTier> Tiers { get; set; } = [];

    /// <summary>The objectives, e.g. a collection's items; progress lists which are done by index.</summary>
    public List<AchievementBit> Bits { get; set; } = [];

    /// <summary>What completing it gives: coins, items, mastery points or a title.</summary>
    public List<AchievementReward> Rewards { get; set; } = [];
}

/// <summary>One tier: the progress count it needs and the points it gives.</summary>
public class AchievementTier
{
    public int Count { get; set; }

    public int Points { get; set; }
}

/// <summary>One objective of an achievement.</summary>
public class AchievementBit
{
    /// <summary>"Text", "Item", "Minipet" or "Skin".</summary>
    [MaxLength(16)]
    public string Type { get; set; } = "";

    /// <summary>The item, minipet or skin id; 0 for text.</summary>
    public int Id { get; set; }

    public string? Text { get; set; }
}

/// <summary>One reward of an achievement.</summary>
public class AchievementReward
{
    /// <summary>"Coins", "Item", "Mastery" or "Title".</summary>
    [MaxLength(16)]
    public string Type { get; set; } = "";

    /// <summary>The item or title id; null for coins and mastery points.</summary>
    public int? Id { get; set; }

    /// <summary>How many items, or the copper for coins.</summary>
    public int? Count { get; set; }

    /// <summary>A mastery point's region, e.g. "Tyria".</summary>
    [MaxLength(32)]
    public string? Region { get; set; }
}
