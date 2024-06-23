namespace Gw2Gizmos.Gw2Api.Contract.Wvw;

public readonly struct WvwObjectiveType : IEquatable<WvwObjectiveType>
{
    public static readonly WvwObjectiveType Camp = new("Camp");
    public static readonly WvwObjectiveType Castle = new("Castle");
    public static readonly WvwObjectiveType Keep = new("Keep");
    public static readonly WvwObjectiveType Mercenary = new("Mercenary");
    public static readonly WvwObjectiveType Ruins = new("Ruins");
    public static readonly WvwObjectiveType Spawn = new("Spawn");
    public static readonly WvwObjectiveType Tower = new("Tower");

    public string Value { get; }

    private WvwObjectiveType(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator WvwObjectiveType(string value) => new(value);

    public static implicit operator string(WvwObjectiveType value) => value.Value;

    public bool Equals(WvwObjectiveType other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is WvwObjectiveType other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(WvwObjectiveType left, WvwObjectiveType right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(WvwObjectiveType left, WvwObjectiveType right)
    {
        return !left.Equals(right);
    }
}
