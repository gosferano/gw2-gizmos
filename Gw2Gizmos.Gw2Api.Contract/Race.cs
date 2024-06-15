namespace Gw2Gizmos.Gw2Api.Contract;

public readonly struct Race : IEquatable<Race>
{
    public static readonly Race Asura = new Race("Asura");
    public static readonly Race Charr = new Race("Charr");
    public static readonly Race Human = new Race("Human");
    public static readonly Race Norn = new Race("Norn");
    public static readonly Race Sylvari = new Race("Sylvari");

    public string Value { get; }

    private Race(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator Race(string value) => new(value);

    public static implicit operator string(Race value) => value.Value;

    public bool Equals(Race other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is Race other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(Race left, Race right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Race left, Race right)
    {
        return !left.Equals(right);
    }
}
