namespace Gw2Gizmos.Gw2Api.Contract.V2.Maps;

public readonly struct MapType : IEquatable<MapType>
{
    public static readonly MapType BlueHome = new MapType("BlueHome");
    public static readonly MapType Center = new MapType("Center");
    public static readonly MapType EdgeOfTheMists = new MapType("EdgeOfTheMists");
    public static readonly MapType GreenHome = new MapType("GreenHome");
    public static readonly MapType Instance = new MapType("Instance");
    public static readonly MapType JumpPuzzle = new MapType("JumpPuzzle");
    public static readonly MapType Public = new MapType("Public");
    public static readonly MapType Pvp = new MapType("Pvp");
    public static readonly MapType RedHome = new MapType("RedHome");
    public static readonly MapType Tutorial = new MapType("Tutorial");
    public static readonly MapType Unknown = new MapType("Unknown");

    public string Value { get; }

    private MapType(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator MapType(string value) => new(value);

    public static implicit operator string(MapType value) => value.Value;

    public bool Equals(MapType other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is MapType other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(MapType left, MapType right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(MapType left, MapType right)
    {
        return !left.Equals(right);
    }
}
