using System.Collections.Generic;

namespace Gw2Gizmos.MumbleLink.Contract;

/// <summary>
/// A character's profession, as the numeric id MumbleLink's identity reports. Unknown ids (a profession added
/// before this list is updated) flow through via the implicit conversion as their raw value rather than throwing.
/// </summary>
public readonly struct Profession : IEquatable<Profession>
{
    public static readonly Profession Guardian = new(1);
    public static readonly Profession Warrior = new(2);
    public static readonly Profession Engineer = new(3);
    public static readonly Profession Ranger = new(4);
    public static readonly Profession Thief = new(5);
    public static readonly Profession Elementalist = new(6);
    public static readonly Profession Mesmer = new(7);
    public static readonly Profession Necromancer = new(8);
    public static readonly Profession Revenant = new(9);

    /// <summary>Every core profession, in id order.</summary>
    public static readonly IReadOnlyList<Profession> All = new[]
    {
        Guardian, Warrior, Engineer, Ranger, Thief,
        Elementalist, Mesmer, Necromancer, Revenant,
    };

    public int Value { get; }

    private Profession(int value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value.ToString();
    }

    public static implicit operator Profession(int value) => new(value);

    public static implicit operator int(Profession value) => value.Value;

    public bool Equals(Profession other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is Profession other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(Profession left, Profession right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Profession left, Profession right)
    {
        return !left.Equals(right);
    }
}
