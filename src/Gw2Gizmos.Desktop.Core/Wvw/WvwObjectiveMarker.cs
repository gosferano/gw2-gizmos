using System;
using System.Collections.Generic;

namespace Gw2Gizmos.Desktop.Core;

/// <summary>A guild, by its name and tag.</summary>
public sealed record WvwGuild(string Name, string Tag);

/// <summary>A WvW objective as the map draws it: where it is, who holds it and since when.</summary>
/// <param name="Type">"Camp", "Tower", "Keep" or "Castle".</param>
/// <param name="Owner">The side holding it; null while neutral.</param>
/// <param name="OwnerName">The holding team's name; null while neutral.</param>
/// <param name="Guild">The guild that claimed it, if any (and its name could be looked up).</param>
/// <param name="PointsTick">The war score it earns its holder every tick.</param>
/// <param name="Tier">How built up it is from yaks: 0 (none) to 3 (fortified).</param>
/// <param name="TierName">The tier's name ("Secured", "Reinforced", "Fortified"); null below the first.</param>
/// <param name="NextTierName">The next tier's name; null once at the top.</param>
/// <param name="YaksToNextTier">How many more yaks the next tier needs; null once at the top.</param>
/// <param name="GuildUpgrades">The names of the upgrades the claiming guild slotted.</param>
public sealed record WvwObjectiveMarker(
    string Id,
    string Name,
    string Type,
    int MapId,
    double X,
    double Y,
    string? IconUrl,
    string ChatLink,
    WvwColor? Owner,
    string? OwnerName,
    DateTimeOffset LastFlipped,
    WvwGuild? Guild,
    int PointsTick,
    int YaksDelivered,
    int Tier = 0,
    string? TierName = null,
    string? NextTierName = null,
    int? YaksToNextTier = null,
    IReadOnlyList<string>? GuildUpgrades = null
)
{
    /// <summary>How long a freshly flipped objective can't be retaken (Righteous Indignation).</summary>
    public static readonly TimeSpan ProtectionTime = TimeSpan.FromMinutes(5);

    /// <summary>How much longer it can't be retaken; null once it can.</summary>
    public TimeSpan? ProtectedFor(DateTimeOffset now)
    {
        TimeSpan left = LastFlipped + ProtectionTime - now;
        return left > TimeSpan.Zero ? left : null;
    }
}
