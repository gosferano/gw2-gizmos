namespace Gw2Gizmos.Gw2Api.Contract.Continents;

public readonly struct ContinentFloorRegionMapPoiType : IEquatable<ContinentFloorRegionMapPoiType>
{
    public static readonly ContinentFloorRegionMapPoiType Landmark = new("landmark");
    public static readonly ContinentFloorRegionMapPoiType Unlock = new("unlock");
    public static readonly ContinentFloorRegionMapPoiType Vista = new("vista");
    public static readonly ContinentFloorRegionMapPoiType Waypoint = new("waypoint");

    public string Value { get; }

    private ContinentFloorRegionMapPoiType(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator ContinentFloorRegionMapPoiType(string value) => new(value);

    public static implicit operator string(ContinentFloorRegionMapPoiType value) => value.Value;

    public bool Equals(ContinentFloorRegionMapPoiType other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is ContinentFloorRegionMapPoiType other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(ContinentFloorRegionMapPoiType left, ContinentFloorRegionMapPoiType right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ContinentFloorRegionMapPoiType left, ContinentFloorRegionMapPoiType right)
    {
        return !left.Equals(right);
    }
}
