namespace Gw2Gizmos.Gw2Api.Contract.V2.Emotes;

public class Emote
{
    public string[] Commands { get; set; } = Array.Empty<string>();
    public string Id { get; set; } = null!;
    public int[] UnlockItems { get; set; } = Array.Empty<int>();
}
