namespace Gw2Gizmos.Gw2Api.Contract.Worlds;

public readonly struct WorldPopulation : IEquatable<WorldPopulation>
{
    public static readonly WorldPopulation Low = new("Low");
    public static readonly WorldPopulation Medium = new("Medium");
    public static readonly WorldPopulation High = new("High");
    public static readonly WorldPopulation VeryHigh = new("VeryHigh");
    public static readonly WorldPopulation Full = new("Full");

    public string Value { get; }

    private WorldPopulation(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator WorldPopulation(string value) => new(value);

    public static implicit operator string(WorldPopulation value) => value.Value;

    public bool Equals(WorldPopulation other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is WorldPopulation other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(WorldPopulation left, WorldPopulation right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(WorldPopulation left, WorldPopulation right)
    {
        return !left.Equals(right);
    }
}
