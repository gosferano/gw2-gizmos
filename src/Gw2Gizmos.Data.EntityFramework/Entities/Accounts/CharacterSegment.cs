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
}
