using Gw2Gizmos.Gw2Api.Contract.Continents;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Continents;

public class ContinentsFloorsClient : BaseBulkAllClient<ContinentFloor, int>, IContinentsFloorsClient
{
    private readonly int _continentId;

    internal ContinentsFloorsClient(HttpClient httpClient, int continentId)
        : base(httpClient)
    {
        _continentId = continentId;
    }

    protected override string UriPath => $"/v2/continents/{_continentId}/floors";

    public IContinentsFloorsIdClient this[int floorId] =>
        new ContinentsFloorsIdClient(HttpClient, _continentId, floorId);
}
