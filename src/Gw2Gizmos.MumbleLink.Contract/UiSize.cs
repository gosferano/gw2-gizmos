using System.Collections.Generic;

namespace Gw2Gizmos.MumbleLink.Contract;

/// <summary>
/// The in-game UI scale setting, as the <c>uisz</c> value in MumbleLink's identity (0..3). Unknown values flow
/// through via the implicit conversion as their raw value.
/// </summary>
public readonly struct UiSize : IEquatable<UiSize>
{
    public static readonly UiSize Small = new(0);
    public static readonly UiSize Normal = new(1);
    public static readonly UiSize Large = new(2);
    public static readonly UiSize Larger = new(3);

    /// <summary>Every UI size, in order from smallest.</summary>
    public static readonly IReadOnlyList<UiSize> All = new[]
    {
        Small, Normal, Large, Larger,
    };

    public int Value { get; }

    private UiSize(int value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value.ToString();
    }

    public static implicit operator UiSize(int value) => new(value);

    public static implicit operator int(UiSize value) => value.Value;

    public bool Equals(UiSize other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is UiSize other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(UiSize left, UiSize right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(UiSize left, UiSize right)
    {
        return !left.Equals(right);
    }
}
