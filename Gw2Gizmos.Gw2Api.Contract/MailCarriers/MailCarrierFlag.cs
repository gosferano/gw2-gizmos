namespace Gw2Gizmos.Gw2Api.Contract.MailCarriers;

public readonly struct MailCarrierFlag : IEquatable<MailCarrierFlag>
{
    public static readonly MailCarrierFlag Default = new MailCarrierFlag("Default");

    public string Value { get; }

    private MailCarrierFlag(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator MailCarrierFlag(string value) => new(value);

    public static implicit operator string(MailCarrierFlag value) => value.Value;

    public bool Equals(MailCarrierFlag other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is MailCarrierFlag other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(MailCarrierFlag left, MailCarrierFlag right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(MailCarrierFlag left, MailCarrierFlag right)
    {
        return !left.Equals(right);
    }
}
