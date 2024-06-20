namespace Gw2Gizmos.Gw2Api.Contract.Pvp;

public readonly struct PvpResult : IEquatable<PvpResult>
{
    public static readonly PvpResult Defeat = new PvpResult("Defeat");
    public static readonly PvpResult Victory = new PvpResult("Victory");

    public string Value { get; }

    private PvpResult(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator PvpResult(string value) => new(value);

    public static implicit operator string(PvpResult value) => value.Value;

    public bool Equals(PvpResult other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is PvpResult other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(PvpResult left, PvpResult right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PvpResult left, PvpResult right)
    {
        return !left.Equals(right);
    }
}