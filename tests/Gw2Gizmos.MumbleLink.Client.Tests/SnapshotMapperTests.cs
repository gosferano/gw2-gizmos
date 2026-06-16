using Gw2Gizmos.MumbleLink.Client.Marshalling;
using Gw2Gizmos.MumbleLink.Contract;

namespace Gw2Gizmos.MumbleLink.Client.Tests;

public class SnapshotMapperTests
{
    [Fact]
    public void Compose_maps_the_block_and_parses_identity_and_context()
    {
        byte[] context = new ContextBuilder().MapId(15).MountIndex((byte)MountType.Raptor.Value).Build();
        byte[] bytes = new LinkedMemBuilder()
            .UiVersion(2)
            .UiTick(99)
            .AvatarPosition(1f, 2f, 3f)
            .Name("Guild Wars 2")
            .Identity("{\"name\":\"Foo\",\"map_id\":15}")
            .ContextLen(48)
            .Context(context)
            .Build();

        MumbleLinkSnapshot snapshot = SnapshotMapper.Compose(LinkedMemParser.Parse(bytes));

        Assert.Equal(2u, snapshot.UiVersion);
        Assert.Equal(99u, snapshot.UiTick);
        Assert.Equal(new Vector3F(1f, 2f, 3f), snapshot.Avatar.Position);
        Assert.Equal("Guild Wars 2", snapshot.Name);
        Assert.NotNull(snapshot.Identity);
        Assert.Equal("Foo", snapshot.Identity!.Name);
        Assert.NotNull(snapshot.Context);
        Assert.Equal(15u, snapshot.Context!.MapId);
        Assert.Equal(MountType.Raptor, snapshot.Context.Mount);
    }

    [Fact]
    public void Compose_leaves_identity_null_when_the_identity_string_is_empty()
    {
        byte[] bytes = new LinkedMemBuilder().UiTick(1).Name("Guild Wars 2").ContextLen(48).Build();

        MumbleLinkSnapshot snapshot = SnapshotMapper.Compose(LinkedMemParser.Parse(bytes));

        Assert.Null(snapshot.Identity);
    }

    [Fact]
    public void Compose_leaves_context_null_when_context_len_is_zero()
    {
        byte[] bytes = new LinkedMemBuilder().UiTick(1).Name("Guild Wars 2").ContextLen(0).Build();

        MumbleLinkSnapshot snapshot = SnapshotMapper.Compose(LinkedMemParser.Parse(bytes));

        Assert.Null(snapshot.Context);
    }
}
