using System.Collections.Generic;

namespace Gw2Gizmos.MumbleLink.Contract;

/// <summary>
/// A character's race, as the numeric id MumbleLink's identity reports. Unknown ids flow through via the implicit
/// conversion as their raw value rather than throwing.
/// </summary>
public readonly struct Race : IEquatable<Race>
{
    public static readonly Race Asura = new(0);
    public static readonly Race Charr = new(1);
    public static readonly Race Human = new(2);
    public static readonly Race Norn = new(3);
    public static readonly Race Sylvari = new(4);

    /// <summary>Every playable race, in id order.</summary>
    public static readonly IReadOnlyList<Race> All = new[]
    {
        Asura, Charr, Human, Norn, Sylvari,
    };

    public int Value { get; }

    private Race(int value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value.ToString();
    }

    public static implicit operator Race(int value) => new(value);

    public static implicit operator int(Race value) => value.Value;

    public bool Equals(Race other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is Race other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(Race left, Race right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Race left, Race right)
    {
        return !left.Equals(right);
    }
}
