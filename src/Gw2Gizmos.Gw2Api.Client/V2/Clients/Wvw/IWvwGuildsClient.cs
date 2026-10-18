using Gw2Gizmos.Gw2Api.Contract.V2.Wvw;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Wvw;

public interface IWvwGuildsClient : IBlobClient<string[]>
{
    public IWvwGuildsRegionClient this[WvwRegion region] { get; }
}
