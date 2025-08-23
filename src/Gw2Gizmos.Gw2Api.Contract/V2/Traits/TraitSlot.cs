namespace Gw2Gizmos.Gw2Api.Contract.V2.Traits;

public readonly struct TraitSlot : IEquatable<TraitSlot>
{
    public static readonly TraitSlot Major = new("Major");
    public static readonly TraitSlot Minor = new("Minor");

    public string Value { get; }

    private TraitSlot(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator TraitSlot(string value) => new(value);

    public static implicit operator string(TraitSlot value) => value.Value;

    public bool Equals(TraitSlot other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is TraitSlot other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(TraitSlot left, TraitSlot right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(TraitSlot left, TraitSlot right)
    {
        return !left.Equals(right);
    }
}
