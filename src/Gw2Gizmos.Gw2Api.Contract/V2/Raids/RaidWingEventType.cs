namespace Gw2Gizmos.Gw2Api.Contract.V2.Raids;

public readonly struct RaidWingEventType : IEquatable<RaidWingEventType>
{
    public static readonly RaidWingEventType Boss = new RaidWingEventType("Boss");
    public static readonly RaidWingEventType Checkpoint = new RaidWingEventType("Checkpoint");

    public string Value { get; }

    private RaidWingEventType(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator RaidWingEventType(string value) => new(value);

    public static implicit operator string(RaidWingEventType value) => value.Value;

    public bool Equals(RaidWingEventType other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is RaidWingEventType other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(RaidWingEventType left, RaidWingEventType right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(RaidWingEventType left, RaidWingEventType right)
    {
        return !left.Equals(right);
    }
}
