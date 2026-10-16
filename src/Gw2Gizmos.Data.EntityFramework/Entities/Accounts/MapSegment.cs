using System;
using Microsoft.EntityFrameworkCore;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Accounts;

/// <summary>
/// One contiguous stretch on a single map within a <see cref="CharacterSegment"/> — the map changes inside a
/// character's play (zone travel, instances, a trip to a lounge and back). <see cref="MapId"/> is the id MumbleLink
/// reports, resolved to a name through the Maps table. <see cref="EndedAtUtc"/> is null for the current map.
/// <see cref="CharacterSegmentId"/> is a plain scoping column (no navigation), like the other session tables, so
/// deleting a segment deletes its map segments explicitly.
/// </summary>
[Index(nameof(CharacterSegmentId))]
public class MapSegment
{
    public long Id { get; set; }

    public long CharacterSegmentId { get; set; }

    public int MapId { get; set; }

    public DateTimeOffset StartedAtUtc { get; set; }

    /// <summary>When the character left the map (or the segment ended), or <c>null</c> while still on it.</summary>
    public DateTimeOffset? EndedAtUtc { get; set; }
}
