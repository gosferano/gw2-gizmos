namespace Gw2Gizmos.Gw2Api.Contract.V2.Wvw;

/// <summary>
/// When something next happens in WvW, per region.
/// </summary>
public sealed class WvwTimer
{
    public DateTimeOffset Na { get; set; }
    public DateTimeOffset Eu { get; set; }
}
