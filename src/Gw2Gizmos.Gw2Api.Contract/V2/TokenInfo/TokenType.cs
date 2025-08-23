namespace Gw2Gizmos.Gw2Api.Contract.V2.TokenInfo;

public struct TokenType
{
    public static readonly TokenType Account = new("APIKey");
    public static readonly TokenType Builds = new("Subtoken");

    public string Value { get; }

    private TokenType(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator TokenType(string value) => new(value);

    public static implicit operator string(TokenType value) => value.Value;

    public bool Equals(TokenType other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is TokenType other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(TokenType left, TokenType right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(TokenType left, TokenType right)
    {
        return !left.Equals(right);
    }
}
