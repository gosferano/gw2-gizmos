namespace Gw2Gizmos.Gw2Api.Client.Clients.Guild;

public interface IGuildClient
{
    public IGuildIdClient this[string guildId] { get; }
}
