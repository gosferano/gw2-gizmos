using Gw2Gizmos.MumbleLink.Client.Marshalling;
using Gw2Gizmos.MumbleLink.Contract;

namespace Gw2Gizmos.MumbleLink.Client.Tests;

public class LinkedMemParserTests
{
    [Fact]
    public void Parse_reads_every_fixed_field()
    {
        byte[] bytes = new LinkedMemBuilder()
            .UiVersion(2)
            .UiTick(123456)
            .AvatarPosition(1f, 2f, 3f)
            .AvatarFront(0f, 0f, 1f)
            .AvatarTop(0f, 1f, 0f)
            .Name("Guild Wars 2")
            .CameraPosition(4f, 5f, 6f)
            .CameraFront(1f, 0f, 0f)
            .CameraTop(0f, 1f, 0f)
            .Identity("{\"name\":\"Foo\"}")
            .ContextLen(48)
            .Description("desc")
            .Build();

        LinkedMem mem = LinkedMemParser.Parse(bytes);

        Assert.Equal(2u, mem.UiVersion);
        Assert.Equal(123456u, mem.UiTick);
        Assert.Equal(new Vector3F(1f, 2f, 3f), mem.AvatarPosition);
        Assert.Equal(new Vector3F(0f, 0f, 1f), mem.AvatarFront);
        Assert.Equal(new Vector3F(0f, 1f, 0f), mem.AvatarTop);
        Assert.Equal("Guild Wars 2", mem.Name);
        Assert.Equal(new Vector3F(4f, 5f, 6f), mem.CameraPosition);
        Assert.Equal(new Vector3F(1f, 0f, 0f), mem.CameraFront);
        Assert.Equal(new Vector3F(0f, 1f, 0f), mem.CameraTop);
        Assert.Equal("{\"name\":\"Foo\"}", mem.Identity);
        Assert.Equal(48u, mem.ContextLen);
        Assert.Equal(256, mem.Context.Length);
        Assert.Equal("desc", mem.Description);
    }

    [Fact]
    public void Parse_trims_strings_at_the_null_terminator()
    {
        byte[] bytes = new LinkedMemBuilder().UiTick(1).Name("Guild Wars 2").Build();

        LinkedMem mem = LinkedMemParser.Parse(bytes);

        // The field is 256 chars wide but the value is short; everything past the NUL must be dropped.
        Assert.Equal("Guild Wars 2", mem.Name);
    }

    [Fact]
    public void Parse_of_a_zero_buffer_yields_empty_strings_and_zero_tick()
    {
        byte[] bytes = new byte[LinkedMem.Size];

        LinkedMem mem = LinkedMemParser.Parse(bytes);

        Assert.Equal(0u, mem.UiTick);
        Assert.Equal("", mem.Name);
        Assert.Equal("", mem.Identity);
    }

    [Fact]
    public void Parse_throws_when_the_buffer_is_too_small()
    {
        byte[] tooSmall = new byte[LinkedMem.Size - 1];

        Assert.Throws<ArgumentException>(() => LinkedMemParser.Parse(tooSmall));
    }
}
