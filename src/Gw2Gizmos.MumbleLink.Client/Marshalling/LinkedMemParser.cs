using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Gw2Gizmos.MumbleLink.Client.Marshalling;

/// <summary>
/// Decodes the fixed-layout <c>LinkedMem</c> bytes into a <see cref="LinkedMem"/>. A pure function over a
/// <see cref="ReadOnlySpan{T}"/> — no OS shared memory — so it is fully unit-testable from a hand-built buffer.
/// </summary>
/// <remarks>
/// Native layout (little-endian, 4-byte aligned, no padding — every field is a multiple of 4 bytes):
/// <code>
///   off  size  field
///     0     4  uint   uiVersion
///     4     4  uint   uiTick
///     8    12  float  fAvatarPosition[3]
///    20    12  float  fAvatarFront[3]
///    32    12  float  fAvatarTop[3]
///    44   512  wchar  name[256]            (UTF-16, null-terminated)
///   556    12  float  fCameraPosition[3]
///   568    12  float  fCameraFront[3]
///   580    12  float  fCameraTop[3]
///   592   512  wchar  identity[256]        (UTF-16 JSON, null-terminated)
///  1104     4  uint   context_len
///  1108   256  byte   context[256]         (game-specific blob)
///  1364  4096  wchar  description[2048]    (UTF-16, null-terminated)
///  5460        = LinkedMem.Size
/// </code>
/// </remarks>
internal static class LinkedMemParser
{
    private const int UiVersionOffset = 0;
    private const int UiTickOffset = 4;
    private const int AvatarPositionOffset = 8;
    private const int AvatarFrontOffset = 20;
    private const int AvatarTopOffset = 32;
    private const int NameOffset = 44;
    private const int CameraPositionOffset = 556;
    private const int CameraFrontOffset = 568;
    private const int CameraTopOffset = 580;
    private const int IdentityOffset = 592;
    private const int ContextLenOffset = 1104;
    private const int ContextOffset = 1108;
    private const int DescriptionOffset = 1364;

    private const int NameChars = 256;
    private const int IdentityChars = 256;
    private const int ContextBytes = 256;
    private const int DescriptionChars = 2048;

    public static LinkedMem Parse(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < LinkedMem.Size)
        {
            throw new ArgumentException(
                $"MumbleLink block is {bytes.Length} bytes; expected at least {LinkedMem.Size}.",
                nameof(bytes)
            );
        }

        return new LinkedMem
        {
            UiVersion = BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(UiVersionOffset, 4)),
            UiTick = BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(UiTickOffset, 4)),
            AvatarPosition = ReadVector3F(bytes, AvatarPositionOffset),
            AvatarFront = ReadVector3F(bytes, AvatarFrontOffset),
            AvatarTop = ReadVector3F(bytes, AvatarTopOffset),
            Name = ReadUtf16(bytes, NameOffset, NameChars),
            CameraPosition = ReadVector3F(bytes, CameraPositionOffset),
            CameraFront = ReadVector3F(bytes, CameraFrontOffset),
            CameraTop = ReadVector3F(bytes, CameraTopOffset),
            Identity = ReadUtf16(bytes, IdentityOffset, IdentityChars),
            ContextLen = BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(ContextLenOffset, 4)),
            Context = bytes.Slice(ContextOffset, ContextBytes).ToArray(),
            Description = ReadUtf16(bytes, DescriptionOffset, DescriptionChars),
        };
    }

    private static Vector3F ReadVector3F(ReadOnlySpan<byte> bytes, int offset)
    {
        return new Vector3F(
            BinaryPrimitives.ReadSingleLittleEndian(bytes.Slice(offset, 4)),
            BinaryPrimitives.ReadSingleLittleEndian(bytes.Slice(offset + 4, 4)),
            BinaryPrimitives.ReadSingleLittleEndian(bytes.Slice(offset + 8, 4))
        );
    }

    // The game writes UTF-16LE, fixed-width and null-terminated; reinterpret the bytes as chars and trim at the
    // first NUL. MemoryMarshal.Cast is safe and assumes little-endian char order, which matches the source.
    private static string ReadUtf16(ReadOnlySpan<byte> bytes, int offset, int charCount)
    {
        ReadOnlySpan<char> chars = MemoryMarshal.Cast<byte, char>(bytes.Slice(offset, charCount * 2));
        int terminator = chars.IndexOf('\0');
        if (terminator >= 0)
        {
            chars = chars[..terminator];
        }

        return new string(chars);
    }
}
