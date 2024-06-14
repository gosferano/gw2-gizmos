namespace Gw2Gizmos.Gw2Api.Client;

public readonly struct Locale : IEquatable<Locale>
{
    public static readonly Locale English = new("en");
    public static readonly Locale German = new("de");
    public static readonly Locale French = new("fr");
    public static readonly Locale Spanish = new("es");
    public static readonly Locale Chinese = new("zh");

    public string Value { get; }

    private Locale(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator Locale(string value) => new(value);

    public static implicit operator string(Locale locale) => locale.Value;

    public bool Equals(Locale other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is Locale other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(Locale left, Locale right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Locale left, Locale right)
    {
        return !left.Equals(right);
    }
}
