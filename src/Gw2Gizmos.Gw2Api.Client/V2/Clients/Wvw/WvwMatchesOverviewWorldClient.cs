using Gw2Gizmos.Gw2Api.Contract.Wvw;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Wvw;

public class WvwMatchesOverviewWorldClient : BaseClient, IWvwMatchesOverviewWorldClient
{
    private readonly int _worldId;

    internal WvwMatchesOverviewWorldClient(HttpClient httpClient, int worldId)
        : base(httpClient)
    {
        _worldId = worldId;
    }

    protected override string UriPath => $"/v2/wvw/matches/overview";

    public Task<WvwMatchOverview> GetBlob(CancellationToken cancellationToken = default)
    {
        return Get<WvwMatchOverview>($"{UriPath}?world={_worldId}", SchemaVersion, cancellationToken);
    }
}
