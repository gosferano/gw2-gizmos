namespace Gw2Gizmos.Gw2Api.Contract.V2;

public readonly struct RaceName : IEquatable<RaceName>
{
    public static readonly RaceName Asura = new RaceName("Asura");
    public static readonly RaceName Charr = new RaceName("Charr");
    public static readonly RaceName Human = new RaceName("Human");
    public static readonly RaceName Norn = new RaceName("Norn");
    public static readonly RaceName Sylvari = new RaceName("Sylvari");

    public string Value { get; }

    private RaceName(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator RaceName(string value) => new(value);

    public static implicit operator string(RaceName value) => value.Value;

    public bool Equals(RaceName other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is RaceName other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(RaceName left, RaceName right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(RaceName left, RaceName right)
    {
        return !left.Equals(right);
    }
}
