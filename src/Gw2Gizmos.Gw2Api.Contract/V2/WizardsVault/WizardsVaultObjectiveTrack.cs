namespace Gw2Gizmos.Gw2Api.Contract.V2.WizardsVault;

public readonly struct WizardsVaultObjectiveTrack : IEquatable<WizardsVaultObjectiveTrack>
{
    public static readonly WizardsVaultObjectiveTrack PvE = new("PvE");
    public static readonly WizardsVaultObjectiveTrack PvP = new("PvP");
    public static readonly WizardsVaultObjectiveTrack WvW = new("WvW");

    public string Value { get; }

    private WizardsVaultObjectiveTrack(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator WizardsVaultObjectiveTrack(string value) => new(value);

    public static implicit operator string(WizardsVaultObjectiveTrack value) => value.Value;

    public bool Equals(WizardsVaultObjectiveTrack other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is WizardsVaultObjectiveTrack other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(WizardsVaultObjectiveTrack left, WizardsVaultObjectiveTrack right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(WizardsVaultObjectiveTrack left, WizardsVaultObjectiveTrack right)
    {
        return !left.Equals(right);
    }
}
