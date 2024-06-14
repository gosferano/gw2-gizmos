namespace Gw2Gizmos.Gw2Api.Contract.Items;

public readonly struct ContainerType : IEquatable<ContainerType>
{
    public static readonly ContainerType Foraging = new("Foraging");
    public static readonly ContainerType Logging = new("Logging");
    public static readonly ContainerType Mining = new("Mining");
    public static readonly ContainerType Bait = new("Bait");
    public static readonly ContainerType Lure = new("Lure");

    public string Value { get; }

    private ContainerType(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator ContainerType(string value) => new(value);

    public bool Equals(ContainerType other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is ContainerType other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(ContainerType left, ContainerType right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ContainerType left, ContainerType right)
    {
        return !left.Equals(right);
    }
}
