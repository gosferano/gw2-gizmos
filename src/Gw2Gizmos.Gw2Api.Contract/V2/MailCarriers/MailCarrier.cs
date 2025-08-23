namespace Gw2Gizmos.Gw2Api.Contract.V2.MailCarriers;

public class MailCarrier
{
    public int Id { get; set; }
    public int[] UnlockItems { get; set; } = Array.Empty<int>();
    public int Order { get; set; }
    public string Icon { get; set; }
    public string Name { get; set; }
    public MailCarrierFlag[] Flags { get; set; } = Array.Empty<MailCarrierFlag>();
}
