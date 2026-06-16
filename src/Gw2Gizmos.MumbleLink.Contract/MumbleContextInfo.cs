namespace Gw2Gizmos.MumbleLink.Contract;

/// <summary>
/// The parsed contents of MumbleLink's game-specific <c>context</c> blob (GW2's <c>MumbleContext</c>) — map and
/// shard identity, the UI-state bitmask, compass geometry, the game process id, and the current mount.
/// </summary>
public sealed class MumbleContextInfo
{
    /// <summary>The raw <c>sockaddr</c> bytes of the map server the client is connected to (28 bytes).</summary>
    public byte[] ServerAddress { get; init; } = Array.Empty<byte>();

    public uint MapId { get; init; }

    public ContextMapType MapType { get; init; }

    public uint ShardId { get; init; }

    public uint Instance { get; init; }

    public uint BuildId { get; init; }

    public UiState UiState { get; init; }

    public ushort CompassWidth { get; init; }

    public ushort CompassHeight { get; init; }

    /// <summary>The compass rotation, in radians.</summary>
    public float CompassRotation { get; init; }

    public float PlayerX { get; init; }

    public float PlayerY { get; init; }

    public float MapCenterX { get; init; }

    public float MapCenterY { get; init; }

    public float MapScale { get; init; }

    /// <summary>The Guild Wars 2 game process id.</summary>
    public uint ProcessId { get; init; }

    public MountType Mount { get; init; }
}
