namespace Gw2Gizmos.Gw2Api.Contract.Guild;

public readonly struct GuildTeamMemberRole : IEquatable<GuildTeamMemberRole>
{
    public static readonly GuildTeamMemberRole Captain = new("Captain");
    public static readonly GuildTeamMemberRole Member = new("Member");

    public string Value { get; }

    private GuildTeamMemberRole(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator GuildTeamMemberRole(string value) => new(value);

    public static implicit operator string(GuildTeamMemberRole value) => value.Value;

    public bool Equals(GuildTeamMemberRole other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is GuildTeamMemberRole other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(GuildTeamMemberRole left, GuildTeamMemberRole right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(GuildTeamMemberRole left, GuildTeamMemberRole right)
    {
        return !left.Equals(right);
    }
}
