namespace Gw2Gizmos.MumbleLink.Client.Marshalling;

/// <summary>
/// The decoded outer <c>LinkedMem</c> block: the fixed-layout part of MumbleLink (everything except the
/// game-specific parsing of <see cref="Identity"/> and <see cref="Context"/>, which the snapshot mapper defers to
/// the identity/context parsers). Produced by <see cref="LinkedMemParser"/>; the byte layout is documented there.
/// </summary>
internal sealed class LinkedMem
{
    /// <summary>The size of the native <c>LinkedMem</c> struct in bytes — the exact length the parser expects.</summary>
    public const int Size = 5460;

    public uint UiVersion { get; init; }
    public uint UiTick { get; init; }
    public Vector3F AvatarPosition { get; init; }
    public Vector3F AvatarFront { get; init; }
    public Vector3F AvatarTop { get; init; }
    public string Name { get; init; } = "";
    public Vector3F CameraPosition { get; init; }
    public Vector3F CameraFront { get; init; }
    public Vector3F CameraTop { get; init; }
    public string Identity { get; init; } = "";

    /// <summary>The game-declared length of the meaningful part of <see cref="Context"/>; 0 until populated.</summary>
    public uint ContextLen { get; init; }

    /// <summary>The raw 256-byte game-specific context blob (GW2's <c>MumbleContext</c>).</summary>
    public byte[] Context { get; init; } = Array.Empty<byte>();

    public string Description { get; init; } = "";
}
