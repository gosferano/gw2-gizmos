namespace Gw2Gizmos.Gw2Api.Contract.V2.MailCarriers;

public sealed class MailCarrier
{
    public int Id { get; set; }
    public int[] UnlockItems { get; set; } = Array.Empty<int>();
    public int Order { get; set; }
    public string Icon { get; set; } = null!;
    public string Name { get; set; } = null!;
    public MailCarrierFlag[] Flags { get; set; } = Array.Empty<MailCarrierFlag>();
}
