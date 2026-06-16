using System.Buffers.Binary;
using System.Text;
using Gw2Gizmos.MumbleLink.Client.Marshalling;

namespace Gw2Gizmos.MumbleLink.Client.Tests;

/// <summary>
/// Builds a byte buffer with a known <c>LinkedMem</c> layout (the analogue of the API client tests'
/// <c>StubHttpMessageHandler</c>) so the parsers can be asserted deterministically without the game or any OS
/// shared memory. Offsets mirror <see cref="LinkedMemParser"/>.
/// </summary>
internal sealed class LinkedMemBuilder
{
    private readonly byte[] _bytes = new byte[LinkedMem.Size];

    public LinkedMemBuilder UiVersion(uint value) => Write32(0, value);

    public LinkedMemBuilder UiTick(uint value) => Write32(4, value);

    public LinkedMemBuilder AvatarPosition(float x, float y, float z) => WriteVector(8, x, y, z);

    public LinkedMemBuilder AvatarFront(float x, float y, float z) => WriteVector(20, x, y, z);

    public LinkedMemBuilder AvatarTop(float x, float y, float z) => WriteVector(32, x, y, z);

    public LinkedMemBuilder Name(string value) => WriteUtf16(44, 256, value);

    public LinkedMemBuilder CameraPosition(float x, float y, float z) => WriteVector(556, x, y, z);

    public LinkedMemBuilder CameraFront(float x, float y, float z) => WriteVector(568, x, y, z);

    public LinkedMemBuilder CameraTop(float x, float y, float z) => WriteVector(580, x, y, z);

    public LinkedMemBuilder Identity(string value) => WriteUtf16(592, 256, value);

    public LinkedMemBuilder ContextLen(uint value) => Write32(1104, value);

    public LinkedMemBuilder Context(ReadOnlySpan<byte> context)
    {
        context.CopyTo(_bytes.AsSpan(1108, 256));
        return this;
    }

    public LinkedMemBuilder Description(string value) => WriteUtf16(1364, 2048, value);

    public byte[] Build() => (byte[])_bytes.Clone();

    private LinkedMemBuilder Write32(int offset, uint value)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(_bytes.AsSpan(offset, 4), value);
        return this;
    }

    private LinkedMemBuilder WriteVector(int offset, float x, float y, float z)
    {
        BinaryPrimitives.WriteSingleLittleEndian(_bytes.AsSpan(offset, 4), x);
        BinaryPrimitives.WriteSingleLittleEndian(_bytes.AsSpan(offset + 4, 4), y);
        BinaryPrimitives.WriteSingleLittleEndian(_bytes.AsSpan(offset + 8, 4), z);
        return this;
    }

    private LinkedMemBuilder WriteUtf16(int offset, int charCount, string value)
    {
        // Mirror the game: fixed-width UTF-16LE, NUL-terminated, truncated to the field width.
        int chars = Math.Min(value.Length, charCount - 1);
        Encoding.Unicode.GetBytes(value.AsSpan(0, chars), _bytes.AsSpan(offset, chars * 2));
        return this;
    }
}

/// <summary>Builds the 256-byte GW2 <c>MumbleContext</c> blob; offsets mirror <see cref="ContextParser"/>.</summary>
internal sealed class ContextBuilder
{
    private readonly byte[] _bytes = new byte[256];

    public ContextBuilder ServerAddress(ReadOnlySpan<byte> address)
    {
        address[..Math.Min(address.Length, 28)].CopyTo(_bytes.AsSpan(0, 28));
        return this;
    }

    public ContextBuilder MapId(uint value) => Write32(28, value);

    public ContextBuilder MapType(uint value) => Write32(32, value);

    public ContextBuilder ShardId(uint value) => Write32(36, value);

    public ContextBuilder Instance(uint value) => Write32(40, value);

    public ContextBuilder BuildId(uint value) => Write32(44, value);

    public ContextBuilder UiState(uint value) => Write32(48, value);

    public ContextBuilder CompassWidth(ushort value) => Write16(52, value);

    public ContextBuilder CompassHeight(ushort value) => Write16(54, value);

    public ContextBuilder CompassRotation(float value) => WriteSingle(56, value);

    public ContextBuilder PlayerX(float value) => WriteSingle(60, value);

    public ContextBuilder PlayerY(float value) => WriteSingle(64, value);

    public ContextBuilder MapCenterX(float value) => WriteSingle(68, value);

    public ContextBuilder MapCenterY(float value) => WriteSingle(72, value);

    public ContextBuilder MapScale(float value) => WriteSingle(76, value);

    public ContextBuilder ProcessId(uint value) => Write32(80, value);

    public ContextBuilder MountIndex(byte value)
    {
        _bytes[84] = value;
        return this;
    }

    public byte[] Build() => (byte[])_bytes.Clone();

    private ContextBuilder Write16(int offset, ushort value)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(_bytes.AsSpan(offset, 2), value);
        return this;
    }

    private ContextBuilder Write32(int offset, uint value)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(_bytes.AsSpan(offset, 4), value);
        return this;
    }

    private ContextBuilder WriteSingle(int offset, float value)
    {
        BinaryPrimitives.WriteSingleLittleEndian(_bytes.AsSpan(offset, 4), value);
        return this;
    }
}
