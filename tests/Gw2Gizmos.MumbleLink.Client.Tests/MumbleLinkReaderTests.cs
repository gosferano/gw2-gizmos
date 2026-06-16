using System.IO.MemoryMappedFiles;
using Gw2Gizmos.MumbleLink.Client.Marshalling;
using Gw2Gizmos.MumbleLink.Contract;

namespace Gw2Gizmos.MumbleLink.Client.Tests;

/// <summary>
/// Exercises the reader against a real named memory-mapped file. The reader uses Windows named shared memory, so
/// these are skipped on other platforms. A private map name is used so a running game's "MumbleLink" is never touched.
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
    public void TryRead_returns_false_when_there_is_no_mapping()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        // A unique name that no process has mapped — the game-closed path.
        string mapName = "Gw2GizmosTest-" + Guid.NewGuid().ToString("N");

        using IMumbleLinkReader reader = new MumbleLinkReader(mapName);

        Assert.False(reader.TryRead(out MumbleLinkSnapshot? snapshot));
        Assert.Null(snapshot);
    }
}
