namespace Gw2Gizmos.Gw2Api.Contract.V2.Items;

public readonly struct ContainerType : IEquatable<ContainerType>
{
    public static readonly ContainerType Default = new("Default");
    public static readonly ContainerType GiftBox = new("GiftBox ");
    public static readonly ContainerType Immediate = new("Immediate");
    public static readonly ContainerType OpenUI = new("OpenUI");

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
