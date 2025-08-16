namespace Gw2Gizmos.Gw2Api.Contract.Account;

public readonly struct AccountWizardsVaultListingType : IEquatable<AccountWizardsVaultListingType>
{
    public static readonly AccountWizardsVaultListingType Featured = new AccountWizardsVaultListingType("Featured");
    public static readonly AccountWizardsVaultListingType Normal = new AccountWizardsVaultListingType("Normal");
    public static readonly AccountWizardsVaultListingType Legacy = new AccountWizardsVaultListingType("Legacy");

    public string Value { get; }

    private AccountWizardsVaultListingType(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator AccountWizardsVaultListingType(string value) => new(value);

    public static implicit operator string(AccountWizardsVaultListingType value) => value.Value;

    public bool Equals(AccountWizardsVaultListingType other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is AccountWizardsVaultListingType other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(AccountWizardsVaultListingType left, AccountWizardsVaultListingType right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(AccountWizardsVaultListingType left, AccountWizardsVaultListingType right)
    {
        return !left.Equals(right);
    }
}
