namespace Gw2Gizmos.Gw2Api.Contract.V2.WizardsVault;

public readonly struct WizardsVaultListingType : IEquatable<WizardsVaultListingType>
{
    public static readonly WizardsVaultListingType Featured = new("Featured");
    public static readonly WizardsVaultListingType Legacy = new("Legacy");
    public static readonly WizardsVaultListingType Normal = new("Normal");

    public string Value { get; }

    private WizardsVaultListingType(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator WizardsVaultListingType(string value) => new(value);

    public static implicit operator string(WizardsVaultListingType value) => value.Value;

    public bool Equals(WizardsVaultListingType other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is WizardsVaultListingType other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(WizardsVaultListingType left, WizardsVaultListingType right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(WizardsVaultListingType left, WizardsVaultListingType right)
    {
        return !left.Equals(right);
    }
}
