namespace Gw2Gizmos.Gw2Api.Contract.V2.Items;

public readonly struct GizmoType : IEquatable<GizmoType>
{
    public static readonly GizmoType Default = new("Default");
    public static readonly GizmoType ContainerKey = new("ContainerKey");
    public static readonly GizmoType RentableContractNpc = new("RentableContractNpc");
    public static readonly GizmoType UnlimitedConsumable = new("UnlimitedConsumable");

    public string Value { get; }

    private GizmoType(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator GizmoType(string value) => new(value);

    public bool Equals(GizmoType other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is GizmoType other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(GizmoType left, GizmoType right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(GizmoType left, GizmoType right)
    {
        return !left.Equals(right);
    }
}
