namespace Gw2Gizmos.Gw2Api.Client.Clients.Home;

public interface IHomeClient : IBlobClient<string[]>
{
    public IHomeCatsClient Cats { get; }
    public IHomeNodesClient Nodes { get; }
}
