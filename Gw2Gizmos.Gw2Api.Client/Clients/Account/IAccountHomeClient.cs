namespace Gw2Gizmos.Gw2Api.Client.Clients.Account;

public interface IAccountHomeClient : IBlobClient<string[]>
{
    public IAccountHomeCatsClient Cats { get; }
    public IAccountHomeNodesClient Nodes { get; }
}
