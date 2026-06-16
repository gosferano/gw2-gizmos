using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Accounts;

/// <summary>
/// One play "sitting" for an account — from the game launching to it closing — detected from MumbleLink (the
/// <c>uiTick</c> advancing, then freezing). It owns an ordered set of <see cref="CharacterSegment"/>s, the
/// stretches each character was active, so the desktop can show both the whole sitting and the switches within it.
/// <see cref="EndedAtUtc"/> is null while the session is still in progress. Keyed by account id so multiple
/// accounts coexist; "hoarded" deltas are reconstructed at read time from the observation logs bracketing the
/// session's start/end timestamps.
/// </summary>
[Index(nameof(AccountId), nameof(StartedAtUtc))]
public class GameSession
{
    public long Id { get; set; }

    [MaxLength(64)]
    public string AccountId { get; set; } = "";

    public DateTimeOffset StartedAtUtc { get; set; }

    /// <summary>When the game closed, or <c>null</c> while the session is still active.</summary>
    public DateTimeOffset? EndedAtUtc { get; set; }
}
