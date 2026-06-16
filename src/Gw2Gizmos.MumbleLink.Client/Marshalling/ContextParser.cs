using System.Buffers.Binary;

namespace Gw2Gizmos.MumbleLink.Client.Marshalling;

/// <summary>
/// Decodes GW2's packed <c>MumbleContext</c> blob (the 256-byte <c>context</c> array) into a
/// <see cref="MumbleContextInfo"/>. A pure function over a <see cref="ReadOnlySpan{T}"/>.
/// </summary>
/// <remarks>
/// Packed layout (little-endian), within the context blob:
/// <code>
///   off  size  field
///     0    28  byte   serverAddress[28]   (sockaddr_in / sockaddr_in6)
///    28     4  uint   mapId
///    32     4  uint   mapType
///    36     4  uint   shardId
///    40     4  uint   instance
///    44     4  uint   buildId
///    48     4  uint   uiState             (bitmask)
///    52     2  ushort compassWidth
///    54     2  ushort compassHeight
///    56     4  float  compassRotation
///    60     4  float  playerX
///    64     4  float  playerY
///    68     4  float  mapCenterX
///    72     4  float  mapCenterY
///    76     4  float  mapScale
///    80     4  uint   processId
///    84     1  byte   mountIndex
/// </code>
/// </remarks>
internal static class ContextParser
{
    private const int ServerAddressOffset = 0;
    private const int ServerAddressLength = 28;
    private const int MapIdOffset = 28;
    private const int MapTypeOffset = 32;
    private const int ShardIdOffset = 36;
    private const int InstanceOffset = 40;
    private const int BuildIdOffset = 44;
    private const int UiStateOffset = 48;
    private const int CompassWidthOffset = 52;
    private const int CompassHeightOffset = 54;
    private const int CompassRotationOffset = 56;
    private const int PlayerXOffset = 60;
    private const int PlayerYOffset = 64;
    private const int MapCenterXOffset = 68;
    private const int MapCenterYOffset = 72;
    private const int MapScaleOffset = 76;
    private const int ProcessIdOffset = 80;
    private const int MountIndexOffset = 84;

    /// <summary>The number of bytes the GW2 layout occupies; the context blob itself is padded to 256.</summary>
    public const int UsedBytes = 85;

    public static MumbleContextInfo Parse(ReadOnlySpan<byte> context)
    {
        if (context.Length < UsedBytes)
        {
            throw new ArgumentException(
                $"MumbleContext is {context.Length} bytes; expected at least {UsedBytes}.",
                nameof(context)
            );
        }

        return new MumbleContextInfo
        {
            ServerAddress = context.Slice(ServerAddressOffset, ServerAddressLength).ToArray(),
            MapId = BinaryPrimitives.ReadUInt32LittleEndian(context.Slice(MapIdOffset, 4)),
            MapType = (int)BinaryPrimitives.ReadUInt32LittleEndian(context.Slice(MapTypeOffset, 4)),
            ShardId = BinaryPrimitives.ReadUInt32LittleEndian(context.Slice(ShardIdOffset, 4)),
            Instance = BinaryPrimitives.ReadUInt32LittleEndian(context.Slice(InstanceOffset, 4)),
            BuildId = BinaryPrimitives.ReadUInt32LittleEndian(context.Slice(BuildIdOffset, 4)),
            UiState = BinaryPrimitives.ReadUInt32LittleEndian(context.Slice(UiStateOffset, 4)),
            CompassWidth = BinaryPrimitives.ReadUInt16LittleEndian(context.Slice(CompassWidthOffset, 2)),
            CompassHeight = BinaryPrimitives.ReadUInt16LittleEndian(context.Slice(CompassHeightOffset, 2)),
            CompassRotation = BinaryPrimitives.ReadSingleLittleEndian(context.Slice(CompassRotationOffset, 4)),
            PlayerX = BinaryPrimitives.ReadSingleLittleEndian(context.Slice(PlayerXOffset, 4)),
            PlayerY = BinaryPrimitives.ReadSingleLittleEndian(context.Slice(PlayerYOffset, 4)),
            MapCenterX = BinaryPrimitives.ReadSingleLittleEndian(context.Slice(MapCenterXOffset, 4)),
            MapCenterY = BinaryPrimitives.ReadSingleLittleEndian(context.Slice(MapCenterYOffset, 4)),
            MapScale = BinaryPrimitives.ReadSingleLittleEndian(context.Slice(MapScaleOffset, 4)),
            ProcessId = BinaryPrimitives.ReadUInt32LittleEndian(context.Slice(ProcessIdOffset, 4)),
            Mount = context[MountIndexOffset],
        };
    }
}
