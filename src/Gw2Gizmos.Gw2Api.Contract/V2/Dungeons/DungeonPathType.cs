namespace Gw2Gizmos.Gw2Api.Contract.V2.Dungeons;

public readonly struct DungeonPathType : IEquatable<DungeonPathType>
{
    public static readonly DungeonPathType Story = new("Story");
    public static readonly DungeonPathType Explorable = new("Explorable");

    public string Value { get; }

    private DungeonPathType(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator DungeonPathType(string value) => new(value);

    public static implicit operator string(DungeonPathType value) => value.Value;

    public bool Equals(DungeonPathType other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is DungeonPathType other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(DungeonPathType left, DungeonPathType right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(DungeonPathType left, DungeonPathType right)
    {
        return !left.Equals(right);
    }
}
