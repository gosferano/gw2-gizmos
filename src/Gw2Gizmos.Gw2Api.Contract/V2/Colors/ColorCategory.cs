namespace Gw2Gizmos.Gw2Api.Contract.V2.Colors;

public readonly struct ColorCategory : IEquatable<ColorCategory>
{
    public static class Hue
    {
        public static readonly ColorCategory Gray = new ColorCategory("Gray");
        public static readonly ColorCategory Brown = new ColorCategory("Brown");
        public static readonly ColorCategory Red = new ColorCategory("Red");
        public static readonly ColorCategory Orange = new ColorCategory("Orange");
        public static readonly ColorCategory Yellow = new ColorCategory("Yellow");
        public static readonly ColorCategory Green = new ColorCategory("Green");
        public static readonly ColorCategory Blue = new ColorCategory("Blue");
        public static readonly ColorCategory Purple = new ColorCategory("Purple");
    }

    public static class Material
    {
        public static readonly ColorCategory Vibrant = new ColorCategory("Vibrant");
        public static readonly ColorCategory Leather = new ColorCategory("Leather");
        public static readonly ColorCategory Metal = new ColorCategory("Metal");
    }

    public static class Rarity
    {
        public static readonly ColorCategory Starter = new ColorCategory("Starter");
        public static readonly ColorCategory Common = new ColorCategory("Common");
        public static readonly ColorCategory Uncommon = new ColorCategory("Uncommon");
        public static readonly ColorCategory Rare = new ColorCategory("Rare");
        public static readonly ColorCategory Exclusive = new ColorCategory("Exclusive");
    }

    public string Value { get; }

    private ColorCategory(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator ColorCategory(string value) => new(value);

    public static implicit operator string(ColorCategory value) => value.Value;

    public bool Equals(ColorCategory other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is ColorCategory other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(ColorCategory left, ColorCategory right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ColorCategory left, ColorCategory right)
    {
        return !left.Equals(right);
    }
}
