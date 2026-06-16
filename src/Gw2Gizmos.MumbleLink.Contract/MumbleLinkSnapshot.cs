namespace Gw2Gizmos.MumbleLink.Contract;

/// <summary>
/// An immutable read of the MumbleLink shared-memory block at one instant: the protocol version, the per-frame
/// <see cref="UiTick"/> (the liveness signal — it advances while the game updates and freezes when it doesn't),
/// the avatar/camera pose, the game name, and the parsed GW2 identity and context.
/// </summary>
public sealed class MumbleLinkSnapshot
{
    public uint UiVersion { get; init; }

    /// <summary>
    /// The frame counter the game increments each positional-audio update. Advancing means the game is live;
    /// a frozen tick means it is not updating (alt-tabbed, loading, or closed).
    /// </summary>
    public uint UiTick { get; init; }

    public AvatarPose Avatar { get; init; } = null!;

    public CameraPose Camera { get; init; } = null!;

    /// <summary>The publishing game's name — "Guild Wars 2" for the GW2 client.</summary>
    public string Name { get; init; } = null!;

    /// <summary>
    /// The parsed identity, or <c>null</c> when the game has not populated it yet (the block exists for a frame
    /// or two before the first character data is written).
    /// </summary>
    public MumbleIdentity? Identity { get; init; }

    /// <summary>The parsed game context, or <c>null</c> when the context length is 0 (not yet populated).</summary>
    public MumbleContextInfo? Context { get; init; }

    /// <summary>A free-form description field; GW2 leaves it empty.</summary>
    public string Description { get; init; } = "";
}
