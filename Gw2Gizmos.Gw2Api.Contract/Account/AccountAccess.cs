namespace Gw2Gizmos.Gw2Api.Contract.Account;

public readonly struct AccountAccess : IEquatable<AccountAccess>
{
    public static readonly AccountAccess GuildWars2 = new AccountAccess("GuildWars2");
    public static readonly AccountAccess HeartOfThorns = new AccountAccess("HeartOfThorns");
    public static readonly AccountAccess PathOfFire = new AccountAccess("PathOfFire");
    public static readonly AccountAccess EndOfDragons = new AccountAccess("EndOfDragons");
    public static readonly AccountAccess SecretsOfTheObscure = new AccountAccess("SecretsOfTheObscure");
    public static readonly AccountAccess JanthirWilds = new AccountAccess("JanthirWilds");

    public string Value { get; }

    private AccountAccess(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator AccountAccess(string value) => new(value);

    public bool Equals(AccountAccess other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is AccountAccess other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(AccountAccess left, AccountAccess right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(AccountAccess left, AccountAccess right)
    {
        return !left.Equals(right);
    }
}
