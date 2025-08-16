namespace Gw2Gizmos.Gw2Api.Contract.Novelties;

public readonly struct NoveltySlot : IEquatable<NoveltySlot>
{
    public static readonly NoveltySlot Chair = new NoveltySlot("Chair");
    public static readonly NoveltySlot Music = new NoveltySlot("Music");
    public static readonly NoveltySlot HeldItem = new NoveltySlot("HeldItem");
    public static readonly NoveltySlot Miscellaneous = new NoveltySlot("Miscellaneous");
    public static readonly NoveltySlot Tonic = new NoveltySlot("Tonic");

    public string Value { get; }

    private NoveltySlot(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator NoveltySlot(string value) => new(value);

    public static implicit operator string(NoveltySlot value) => value.Value;

    public bool Equals(NoveltySlot other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is NoveltySlot other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(NoveltySlot left, NoveltySlot right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(NoveltySlot left, NoveltySlot right)
    {
        return !left.Equals(right);
    }
}