namespace Gw2Gizmos.Gw2Api.Contract.V2;

/// <summary>
/// A release by its display name, e.g. "Central Tyria" or "Heart of Thorns".
/// </summary>
public readonly struct ReleaseName : IEquatable<ReleaseName>
{
    public static readonly ReleaseName CentralTyria = new ReleaseName("Central Tyria");
    public static readonly ReleaseName HeartOfThorns = new ReleaseName("Heart of Thorns");
    public static readonly ReleaseName PathOfFire = new ReleaseName("Path of Fire");
    public static readonly ReleaseName IcebroodSaga = new ReleaseName("Icebrood Saga");
    public static readonly ReleaseName EndOfDragons = new ReleaseName("End of Dragons");
    public static readonly ReleaseName SecretsOfTheObscure = new ReleaseName("Secrets of the Obscure");
    public static readonly ReleaseName JanthirWilds = new ReleaseName("Janthir Wilds");
    public static readonly ReleaseName VisionsOfEternity = new ReleaseName("Visions of Eternity");

    public string Value { get; }

    private ReleaseName(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator ReleaseName(string value) => new(value);

    public static implicit operator string(ReleaseName value) => value.Value;

    public bool Equals(ReleaseName other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is ReleaseName other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(ReleaseName left, ReleaseName right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ReleaseName left, ReleaseName right)
    {
        return !left.Equals(right);
    }
}
