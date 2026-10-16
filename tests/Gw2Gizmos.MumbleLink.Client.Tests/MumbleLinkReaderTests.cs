using System.IO.MemoryMappedFiles;
using Gw2Gizmos.MumbleLink.Client.Marshalling;
using Gw2Gizmos.MumbleLink.Contract;

namespace Gw2Gizmos.MumbleLink.Client.Tests;

/// <summary>
/// Exercises the reader against real sources: a named memory-mapped file (Windows-only, so skipped elsewhere; a private
/// map name keeps a running game's "MumbleLink" untouched) and a bridge file (any platform).
/// </summary>
public class MumbleLinkReaderTests
{
    [Fact]
    public void Read_returns_a_snapshot_from_a_live_mapping()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        string mapName = "Gw2GizmosTest-" + Guid.NewGuid().ToString("N");
        byte[] context = new ContextBuilder().MapId(15).MountIndex((byte)MountType.Raptor.Value).Build();
        byte[] block = new LinkedMemBuilder()
            .UiVersion(2)
            .UiTick(4242)
            .Name("Guild Wars 2")
            .Identity("{\"name\":\"Foo\",\"profession\":4}")
            .ContextLen(48)
            .Context(context)
            .Build();

        using MemoryMappedFile mapping = MemoryMappedFile.CreateNew(mapName, LinkedMem.Size);
        using (MemoryMappedViewAccessor writer = mapping.CreateViewAccessor())
        {
            writer.WriteArray(0, block, 0, block.Length);
        }

        using IMumbleLinkReader reader = new MumbleLinkReader(mapName);
        MumbleLinkSnapshot? snapshot = reader.Read();

        Assert.NotNull(snapshot);
        Assert.Equal(4242u, snapshot!.UiTick);
        Assert.Equal("Guild Wars 2", snapshot.Name);
        Assert.Equal("Foo", snapshot.Identity!.Name);
        Assert.Equal(Profession.Ranger, snapshot.Identity.Profession);
        Assert.Equal(15u, snapshot.Context!.MapId);
        Assert.Equal(MountType.Raptor, snapshot.Context.Mount);
    }

    [Fact]
    public void TryRead_returns_false_until_the_game_writes()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        // A unique name that no process has mapped — the game-closed path. The reader creates it, all zero.
        string mapName = "Gw2GizmosTest-" + Guid.NewGuid().ToString("N");

        using IMumbleLinkReader reader = new MumbleLinkReader(mapName);

        Assert.False(reader.TryRead(out MumbleLinkSnapshot? snapshot));
        Assert.Null(snapshot);
    }

    [Fact]
    public void Read_creates_the_mapping_for_the_game_to_write_into()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        string mapName = "Gw2GizmosTest-" + Guid.NewGuid().ToString("N");
        using IMumbleLinkReader reader = new MumbleLinkReader(mapName);
        Assert.Null(reader.Read());

        // The game only opens an existing "MumbleLink"; this one exists because the reader created it.
        byte[] block = new LinkedMemBuilder().UiVersion(2).UiTick(7).Name("Guild Wars 2").Build();
        using (MemoryMappedFile game = MemoryMappedFile.OpenExisting(mapName, MemoryMappedFileRights.ReadWrite))
        using (MemoryMappedViewAccessor writer = game.CreateViewAccessor())
        {
            writer.WriteArray(0, block, 0, block.Length);
        }

        Assert.Equal(7u, reader.Read()?.UiTick);
    }

    [Fact]
    public void ReadForFile_returns_a_snapshot_and_follows_updates()
    {
        string path = Path.Combine(Path.GetTempPath(), "gw2gizmos-mumblelink-" + Guid.NewGuid().ToString("N"));
        try
        {
            File.WriteAllBytes(path, new LinkedMemBuilder().UiVersion(2).UiTick(10).Name("Guild Wars 2").Build());
            using IMumbleLinkReader reader = MumbleLinkReader.CreateForFile(path);

            Assert.Equal(10u, reader.Read()!.UiTick);

            // The bridge rewrites the same file in place; the open mapping sees the new block.
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
            {
                byte[] next = new LinkedMemBuilder().UiVersion(2).UiTick(11).Name("Guild Wars 2").Build();
                stream.Write(next, 0, next.Length);
            }

            MumbleLinkSnapshot? snapshot = reader.Read();
            Assert.NotNull(snapshot);
            Assert.Equal(11u, snapshot!.UiTick);
            Assert.Equal("Guild Wars 2", snapshot.Name);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ReadForFile_returns_null_until_the_bridge_has_written_a_full_block()
    {
        string path = Path.Combine(Path.GetTempPath(), "gw2gizmos-mumblelink-" + Guid.NewGuid().ToString("N"));
        try
        {
            using IMumbleLinkReader reader = MumbleLinkReader.CreateForFile(path);
            Assert.False(reader.TryRead(out _)); // no file yet

            File.WriteAllBytes(path, new byte[100]);
            Assert.False(reader.TryRead(out _)); // too short

            File.WriteAllBytes(path, new LinkedMemBuilder().UiTick(5).Name("Guild Wars 2").Build());
            Assert.True(reader.TryRead(out MumbleLinkSnapshot? snapshot));
            Assert.Equal(5u, snapshot!.UiTick);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
