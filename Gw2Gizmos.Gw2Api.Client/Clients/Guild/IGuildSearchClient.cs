namespace Gw2Gizmos.Gw2Api.Client.Clients.Guild;

public interface IGuildSearchClient
{
    public IGuildSearchNameClient this[string guildName] { get; }
}
