namespace Gw2Gizmos.Gw2Api.Contract.V2.Guild;

public sealed class GuildEmblemComponent
{
    public int Id { get; set; }
    public int[] Colors { get; set; } = Array.Empty<int>();
}
