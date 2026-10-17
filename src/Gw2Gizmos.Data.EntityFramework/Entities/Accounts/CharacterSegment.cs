using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Accounts;

/// <summary>
/// One contiguous stretch a single character was active within a <see cref="GameSession"/>. Consecutive segments
/// (by <see cref="Sequence"/>) are the character switches — the boundary between segment <c>n</c> and <c>n+1</c>
/// is a switch event, and "what happened between switches" is that segment's reconstructed delta. Profession/race
/// aren't stored — they're immutable per character (join the <see cref="Character"/> table by
/// <see cref="CharacterName"/>); world is account state. <see cref="EndedAtUtc"/> is null for the active segment.
/// <see cref="GameSessionId"/> is a plain scoping column (no navigation), consistent with the other account tables.
/// </summary>
[Index(nameof(GameSessionId), nameof(Sequence))]
public class CharacterSegment
{
    public long Id { get; set; }

    public long GameSessionId { get; set; }

    /// <summary>0-based order of this segment within its game session.</summary>
    public int Sequence { get; set; }

    [MaxLength(64)]
    public string CharacterName { get; set; } = "";

    public DateTimeOffset StartedAtUtc { get; set; }

    /// <summary>When the player switched away (or the game closed), or <c>null</c> while this character is active.</summary>
    public DateTimeOffset? EndedAtUtc { get; set; }

    /// <summary>
    /// Total time spent out of the world during this segment — on character select, with the game minimised or on a
    /// long loading screen — when the player came back on the same character. Not playing, so it's left out of
    /// in-world time.
    /// </summary>
    public TimeSpan AwayTime { get; set; }

    /// <summary>
    /// Since when the player has been out of the world, while they still are (character select, minimised); <c>null</c>
    /// while in the world. If they come back on this character it's added to <see cref="AwayTime"/>; otherwise the
    /// segment ends at this moment.
    /// </summary>
    public DateTimeOffset? AwaySinceUtc { get; set; }

    /// <summary>Time actually played: from start to end (or to now, or to leaving the world), minus the time away.</summary>
    public TimeSpan InWorldTime(DateTimeOffset now) =>
        ((AwaySinceUtc ?? EndedAtUtc ?? now) - StartedAtUtc - AwayTime) is { Ticks: > 0 } time ? time : TimeSpan.Zero;
}
