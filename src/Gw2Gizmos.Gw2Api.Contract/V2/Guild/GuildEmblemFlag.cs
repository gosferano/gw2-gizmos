namespace Gw2Gizmos.Gw2Api.Contract.V2.Guild;

public readonly struct GuildEmblemFlag : IEquatable<GuildEmblemFlag>
{
    public static readonly GuildEmblemFlag FlipBackgroundHorizontal = new GuildEmblemFlag("FlipBackgroundHorizontal");
    public static readonly GuildEmblemFlag FlipBackgroundVertical = new GuildEmblemFlag("FlipBackgroundVertical");
    public static readonly GuildEmblemFlag FlipForegroundHorizontal = new GuildEmblemFlag("FlipForegroundHorizontal");
    public static readonly GuildEmblemFlag FlipForegroundVertical = new GuildEmblemFlag("FlipForegroundVertical");

    public string Value { get; }

    private GuildEmblemFlag(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator GuildEmblemFlag(string value) => new(value);

    public static implicit operator string(GuildEmblemFlag value) => value.Value;

    public bool Equals(GuildEmblemFlag other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is GuildEmblemFlag other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(GuildEmblemFlag left, GuildEmblemFlag right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(GuildEmblemFlag left, GuildEmblemFlag right)
    {
        return !left.Equals(right);
    }
}
