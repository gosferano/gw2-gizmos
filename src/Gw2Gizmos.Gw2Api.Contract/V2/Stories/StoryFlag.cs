namespace Gw2Gizmos.Gw2Api.Contract.V2.Stories;

public readonly struct StoryFlag
{
    public static readonly StoryFlag RequiresUnlock = new StoryFlag("RequiresUnlock");

    public string Value { get; }

    private StoryFlag(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator StoryFlag(string value) => new(value);

    public static implicit operator string(StoryFlag value) => value.Value;

    public bool Equals(StoryFlag other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is StoryFlag other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(StoryFlag left, StoryFlag right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(StoryFlag left, StoryFlag right)
    {
        return !left.Equals(right);
    }
}
