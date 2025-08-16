namespace Gw2Gizmos.Gw2Api.Contract.TokenInfo;

public struct TokenPermission : IEquatable<TokenPermission>
{
    public static readonly TokenPermission Account = new("account");
    public static readonly TokenPermission Builds = new("builds");
    public static readonly TokenPermission Characters = new("characters");
    public static readonly TokenPermission Guilds = new("guilds");
    public static readonly TokenPermission Inventories = new("inventories");
    public static readonly TokenPermission Progression = new("progression");
    public static readonly TokenPermission Pvp = new("pvp");
    public static readonly TokenPermission TradingPost = new("tradingpost");
    public static readonly TokenPermission Unlocks = new("unlocks");
    public static readonly TokenPermission Wallet = new("wallet");

    public string Value { get; }

    private TokenPermission(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator TokenPermission(string value) => new(value);

    public static implicit operator string(TokenPermission value) => value.Value;

    public bool Equals(TokenPermission other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is TokenPermission other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(TokenPermission left, TokenPermission right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(TokenPermission left, TokenPermission right)
    {
        return !left.Equals(right);
    }
}