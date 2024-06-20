namespace Gw2Gizmos.Gw2Api.Contract.Pvp;

public readonly struct PvpTeam
{
    public static readonly PvpTeam Blue = new("Blue");
    public static readonly PvpTeam Red = new("Red");

    public string Value { get; }

    private PvpTeam(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator PvpTeam(string value) => new(value);

    public static implicit operator string(PvpTeam value) => value.Value;

    public bool Equals(PvpTeam other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is PvpTeam other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(PvpTeam left, PvpTeam right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PvpTeam left, PvpTeam right)
    {
        return !left.Equals(right);
    }
}