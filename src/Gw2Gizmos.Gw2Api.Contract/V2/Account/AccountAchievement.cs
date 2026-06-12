namespace Gw2Gizmos.Gw2Api.Contract.V2.Account;

public sealed class AccountAchievement
{
    public int Id { get; set; }
    public int[] Bits { get; set; } = Array.Empty<int>();
    public int? Current { get; set; }
    public int? Max { get; set; }
    public bool Done { get; set; }
    public int? Repeated { get; set; }
    public bool? Unlocked { get; set; }
}
