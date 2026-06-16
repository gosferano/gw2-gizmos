using Gw2Gizmos.MumbleLink.Client.Marshalling;
using Gw2Gizmos.MumbleLink.Contract;

namespace Gw2Gizmos.MumbleLink.Client.Tests;

public class ContextParserTests
{
    [Fact]
    public void Parse_reads_all_context_fields()
    {
        byte[] context = new ContextBuilder()
            .ServerAddress(new byte[] { 1, 2, 3, 4 })
            .MapId(15)
            .MapType((uint)ContextMapType.Public.Value)
            .ShardId(99)
            .Instance(7)
            .BuildId(123456)
            .UiState(0)
            .CompassWidth(362)
            .CompassHeight(338)
            .CompassRotation(1.5f)
            .PlayerX(10f)
            .PlayerY(20f)
            .MapCenterX(30f)
            .MapCenterY(40f)
            .MapScale(2.5f)
            .ProcessId(4242)
            .MountIndex((byte)MountType.Skyscale.Value)
            .Build();

        MumbleContextInfo info = ContextParser.Parse(context);

        Assert.Equal(28, info.ServerAddress.Length);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, info.ServerAddress[..4]);
        Assert.Equal(15u, info.MapId);
        Assert.Equal(ContextMapType.Public, info.MapType);
        Assert.Equal(99u, info.ShardId);
        Assert.Equal(7u, info.Instance);
        Assert.Equal(123456u, info.BuildId);
        Assert.Equal((ushort)362, info.CompassWidth);
        Assert.Equal((ushort)338, info.CompassHeight);
        Assert.Equal(1.5f, info.CompassRotation, 0.0001f);
        Assert.Equal(10f, info.PlayerX, 0.0001f);
        Assert.Equal(20f, info.PlayerY, 0.0001f);
        Assert.Equal(30f, info.MapCenterX, 0.0001f);
        Assert.Equal(40f, info.MapCenterY, 0.0001f);
        Assert.Equal(2.5f, info.MapScale, 0.0001f);
        Assert.Equal(4242u, info.ProcessId);
        Assert.Equal(MountType.Skyscale, info.Mount);
    }

    [Fact]
    public void Parse_decodes_the_ui_state_bitmask()
    {
        // bits 0 (IsMapOpen), 3 (GameHasFocus), 6 (IsInCombat) set => 0b100_1001 = 0x49.
        byte[] context = new ContextBuilder().UiState(0b100_1001).Build();

        UiState state = ContextParser.Parse(context).UiState;

        Assert.True(state.IsMapOpen);
        Assert.True(state.GameHasFocus);
        Assert.True(state.IsInCombat);
        Assert.False(state.IsCompassTopRight);
        Assert.False(state.DoesCompassHaveRotationEnabled);
        Assert.False(state.IsInCompetitiveGameMode);
        Assert.False(state.TextboxHasFocus);
    }

    [Fact]
    public void Parse_of_on_foot_yields_mount_none()
    {
        MumbleContextInfo info = ContextParser.Parse(new ContextBuilder().Build());

        Assert.Equal(MountType.None, info.Mount);
    }

    [Fact]
    public void Parse_throws_when_the_context_is_too_small()
    {
        byte[] tooSmall = new byte[ContextParser.UsedBytes - 1];

        Assert.Throws<ArgumentException>(() => ContextParser.Parse(tooSmall));
    }
}
