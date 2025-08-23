using Gw2Gizmos.Gw2Api.Contract.V2.Wvw;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Wvw;

public class WvwMatchesScoresWorldClient : BaseClient, IWvwMatchesScoresWorldClient
{
    private readonly int _worldId;

    internal WvwMatchesScoresWorldClient(HttpClient httpClient, int worldId)
        : base(httpClient)
    {
        _worldId = worldId;
    }

    protected override string UriPath => $"/v2/wvw/matches/scores";

    public Task<Result<WvwMatchScores, Error>> GetBlob(CancellationToken cancellationToken = default)
    {
        return Get<WvwMatchScores>($"{UriPath}?world={_worldId}", SchemaVersion, cancellationToken);
    }
}
