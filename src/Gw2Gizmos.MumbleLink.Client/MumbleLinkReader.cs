using System.Diagnostics.CodeAnalysis;
using System.IO.MemoryMappedFiles;
using System.Runtime.Versioning;
using Gw2Gizmos.MumbleLink.Client.Marshalling;

namespace Gw2Gizmos.MumbleLink.Client;

/// <summary>
/// The default <see cref="IMumbleLinkReader"/>. Opens the named memory-mapped file the GW2 client publishes
/// (read-only — it never creates the file) and keeps the view open across reads, re-opening only after a failed
/// read so a game restart (which replaces the mapping) is picked up automatically. Windows-only.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class MumbleLinkReader : IMumbleLinkReader
{
    internal const string MapName = "MumbleLink";

    private readonly string _mapName;
    private readonly byte[] _buffer = new byte[LinkedMem.Size];
    private MemoryMappedFile? _mappedFile;
    private MemoryMappedViewAccessor? _view;
    private bool _disposed;

    internal MumbleLinkReader()
        : this(MapName) { }

    // Map name is injectable so tests can point at a private mapping instead of the live "MumbleLink".
    internal MumbleLinkReader(string mapName)
    {
        _mapName = mapName;
    }

    /// <summary>Creates a reader without dependency injection (for CLIs, scripts, and the sandbox).</summary>
    public static IMumbleLinkReader Create()
    {
        return new MumbleLinkReader();
    }

    public MumbleLinkSnapshot? Read()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!EnsureOpen())
        {
            return null;
        }

        try
        {
            _view!.ReadArray(0, _buffer, 0, _buffer.Length);
            LinkedMem mem = LinkedMemParser.Parse(_buffer);

            // The mapping exists for a frame or two before the game writes anything — treat that as "not ready".
            if (mem.UiTick == 0 && mem.Name.Length == 0)
            {
                return null;
            }

            return SnapshotMapper.Compose(mem);
        }
        catch (Exception)
        {
            // The game closed or remapped mid-read; drop the handles so the next call re-opens cleanly.
            CloseView();
            return null;
        }
    }

    public bool TryRead([NotNullWhen(true)] out MumbleLinkSnapshot? snapshot)
    {
        snapshot = Read();
        return snapshot is not null;
    }

    private bool EnsureOpen()
    {
        if (_view is not null)
        {
            return true;
        }

        try
        {
            _mappedFile = MemoryMappedFile.OpenExisting(_mapName, MemoryMappedFileRights.Read);
            _view = _mappedFile.CreateViewAccessor(0, LinkedMem.Size, MemoryMappedFileAccess.Read);
            return true;
        }
        // FileNotFoundException: game not running (no mapped file). IOException / UnauthorizedAccessException:
        // the brief race around game start/stop. All are "not available yet", not caller-facing errors.
        catch (Exception ex) when (ex is FileNotFoundException or IOException or UnauthorizedAccessException)
        {
            CloseView();
            return false;
        }
    }

    private void CloseView()
    {
        _view?.Dispose();
        _mappedFile?.Dispose();
        _view = null;
        _mappedFile = null;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        CloseView();
    }
}
