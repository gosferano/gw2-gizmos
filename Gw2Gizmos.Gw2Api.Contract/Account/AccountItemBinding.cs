namespace Gw2Gizmos.Gw2Api.Contract.Account;

public readonly struct AccountItemBinding : IEquatable<AccountItemBinding>
{
    public static readonly AccountItemBinding Account = new AccountItemBinding("Account");
    public static readonly AccountItemBinding Character = new AccountItemBinding("Character");

    public string Value { get; }

    private AccountItemBinding(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator AccountItemBinding(string value) => new(value);

    public bool Equals(AccountItemBinding other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is AccountItemBinding other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(AccountItemBinding left, AccountItemBinding right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(AccountItemBinding left, AccountItemBinding right)
    {
        return !left.Equals(right);
    }
}
