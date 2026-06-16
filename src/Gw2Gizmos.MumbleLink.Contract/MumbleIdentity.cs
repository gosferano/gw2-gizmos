namespace Gw2Gizmos.MumbleLink.Contract;

/// <summary>
/// The parsed contents of MumbleLink's <c>identity</c> field — the GW2-specific JSON blob describing the active
/// character and a few client settings. Present only once the game has populated the block for the current character.
/// </summary>
public sealed class MumbleIdentity
{
    /// <summary>The active character's name.</summary>
    public string Name { get; init; } = null!;

    public Profession Profession { get; init; }

    /// <summary>The active elite specialization id, or <see cref="EliteSpecialization.None"/> when none is equipped.</summary>
    public EliteSpecialization Spec { get; init; }

    public Race Race { get; init; }

    public int MapId { get; init; }

    /// <summary>The world (home server / shard) id.</summary>
    public int WorldId { get; init; }

    public int TeamColorId { get; init; }

    public bool Commander { get; init; }

    /// <summary>The camera field of view, in radians.</summary>
    public float Fov { get; init; }

    public UiSize UiSize { get; init; }
}
