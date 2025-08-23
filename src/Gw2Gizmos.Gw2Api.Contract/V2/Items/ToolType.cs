namespace Gw2Gizmos.Gw2Api.Contract.V2.Items;

public readonly struct ToolType : IEquatable<ToolType>
{
    public static readonly ToolType Salvage = new("Salvage");

    public string Value { get; }

    private ToolType(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator ToolType(string value) => new(value);

    public static implicit operator string(ToolType toolType) => toolType.Value;

    public bool Equals(ToolType other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is ToolType other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(ToolType left, ToolType right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ToolType left, ToolType right)
    {
        return !left.Equals(right);
    }
}
