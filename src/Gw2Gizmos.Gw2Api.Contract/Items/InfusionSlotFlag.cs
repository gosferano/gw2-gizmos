namespace Gw2Gizmos.Gw2Api.Contract.Items;

public readonly struct InfusionSlotFlag : IEquatable<InfusionSlotFlag>
{
    public static readonly InfusionSlotFlag Enrichment = new("Enrichment");
    public static readonly InfusionSlotFlag Infusion = new("Infusion");

    public string Value { get; }

    private InfusionSlotFlag(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator InfusionSlotFlag(string value) => new(value);

    public bool Equals(InfusionSlotFlag other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is InfusionSlotFlag other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(InfusionSlotFlag left, InfusionSlotFlag right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(InfusionSlotFlag left, InfusionSlotFlag right)
    {
        return !left.Equals(right);
    }
}
