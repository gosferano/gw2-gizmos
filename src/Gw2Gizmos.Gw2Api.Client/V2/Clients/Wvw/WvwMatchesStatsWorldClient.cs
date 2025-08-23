using Gw2Gizmos.Gw2Api.Contract.V2.Wvw;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Wvw;

public class WvwMatchesStatsWorldClient : BaseClient, IWvwMatchesStatsWorldClient
{
    private readonly int _worldId;

    internal WvwMatchesStatsWorldClient(HttpClient httpClient, int worldId)
        : base(httpClient)
    {
        _worldId = worldId;
    }

    protected override string UriPath => $"/v2/wvw/matches/stats";

    public Task<Result<WvwMatchStats, Error>> GetBlob(CancellationToken cancellationToken = default)
    {
        return Get<WvwMatchStats>($"{UriPath}?world={_worldId}", SchemaVersion, cancellationToken);
    }
}
