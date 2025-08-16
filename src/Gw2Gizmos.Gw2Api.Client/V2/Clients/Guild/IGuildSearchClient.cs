namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Guild;

public interface IGuildSearchClient
{
    public IGuildSearchNameClient this[string guildName] { get; }
}
