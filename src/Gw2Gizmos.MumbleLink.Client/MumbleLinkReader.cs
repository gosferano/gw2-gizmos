using System.Diagnostics.CodeAnalysis;
using System.IO.MemoryMappedFiles;
using System.Runtime.Versioning;
using Gw2Gizmos.MumbleLink.Client.Marshalling;

namespace Gw2Gizmos.MumbleLink.Client;

/// <summary>
/// The default <see cref="IMumbleLinkReader"/>. Reads the block the GW2 client publishes — on Windows straight from its
/// named shared memory (<see cref="Create"/>); on Linux, where the game runs under Wine/Proton and that memory lives
/// inside Wine, from a file a bridge inside the game's prefix keeps in sync (<see cref="CreateForFile"/>). It never
/// creates the source, keeps the view open across reads, and re-opens only after a failed read, so a game restart is
/// picked up automatically.
/// </summary>
public sealed class MumbleLinkReader : IMumbleLinkReader
{
    internal const string MapName = "MumbleLink";

    private readonly Func<MemoryMappedFile> _open;
    private readonly byte[] _buffer = new byte[LinkedMem.Size];
    private MemoryMappedFile? _mappedFile;
    private MemoryMappedViewAccessor? _view;
    private bool _disposed;

    // Map name is injectable so tests can point at a private mapping instead of the live "MumbleLink".
    [SupportedOSPlatform("windows")]
    internal MumbleLinkReader(string mapName)
        : this(() => MemoryMappedFile.OpenExisting(mapName, MemoryMappedFileRights.Read)) { }

    private MumbleLinkReader(Func<MemoryMappedFile> open)
    {
        _open = open;
    }

    /// <summary>A reader over the game's named shared memory (Windows), without dependency injection.</summary>
    [SupportedOSPlatform("windows")]
    public static IMumbleLinkReader Create()
    {
        return new MumbleLinkReader(MapName);
    }

    /// <summary>
    /// A reader over a file holding a copy of the block (kept in sync by a bridge running next to the game, e.g. inside
    /// its Wine prefix). The file may not exist yet; it's opened on the first successful read.
    /// </summary>
    public static IMumbleLinkReader CreateForFile(string path)
    {
        return new MumbleLinkReader(() => OpenFile(path));
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
            _mappedFile = _open();
            _view = _mappedFile.CreateViewAccessor(0, LinkedMem.Size, MemoryMappedFileAccess.Read);
            return true;
        }
        // FileNotFoundException: game (or bridge) not running. IOException / UnauthorizedAccessException: the brief race
        // around game start/stop, or a bridge file still shorter than the block. All are "not available yet", not
        // caller-facing errors.
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException or IOException
                                       or UnauthorizedAccessException or ArgumentException)
        {
            CloseView();
            return false;
        }
    }

    /// <summary>Maps the bridge file read-only, sharing it fully so the bridge can keep writing (and re-creating) it.</summary>
    private static MemoryMappedFile OpenFile(string path)
    {
        var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        try
        {
            if (stream.Length < LinkedMem.Size)
            {
                throw new IOException("The MumbleLink file is shorter than a LinkedMem block.");
            }

            return MemoryMappedFile.CreateFromFile(
                stream, mapName: null, capacity: 0, MemoryMappedFileAccess.Read, HandleInheritability.None, leaveOpen: false);
        }
        catch
        {
            stream.Dispose();
            throw;
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
