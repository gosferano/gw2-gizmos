using Gw2Gizmos.MumbleLink.Client.Marshalling;
using Gw2Gizmos.MumbleLink.Contract;

namespace Gw2Gizmos.MumbleLink.Client.Tests;

public class IdentityParserTests
{
    private const string SampleJson =
        """
        {"name":"Foo","profession":4,"spec":55,"race":3,"map_id":15,"world_id":1001,"team_color_id":0,"commander":false,"fov":1.222,"uisz":1}
        """;

    [Fact]
    public void Parse_reads_all_identity_fields()
    {
        MumbleIdentity? identity = IdentityParser.Parse(SampleJson);

        Assert.NotNull(identity);
        Assert.Equal("Foo", identity!.Name);
        Assert.Equal(Profession.Ranger, identity.Profession);
        Assert.Equal(55, identity.Spec.Value);
        Assert.Equal(Race.Norn, identity.Race);
        Assert.Equal(15, identity.MapId);
        Assert.Equal(1001, identity.WorldId);
        Assert.Equal(0, identity.TeamColorId);
        Assert.False(identity.Commander);
        Assert.Equal(1.222f, identity.Fov, 0.0001f);
        Assert.Equal(UiSize.Normal, identity.UiSize);
    }

    [Fact]
    public void Parse_of_empty_or_whitespace_returns_null()
    {
        Assert.Null(IdentityParser.Parse(""));
        Assert.Null(IdentityParser.Parse("   "));
    }

    [Fact]
    public void Parse_of_malformed_json_returns_null_without_throwing()
    {
        Assert.Null(IdentityParser.Parse("{not valid json"));
    }
}
