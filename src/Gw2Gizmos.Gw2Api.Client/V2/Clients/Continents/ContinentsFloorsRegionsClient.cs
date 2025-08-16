using Gw2Gizmos.Gw2Api.Contract.Continents;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Continents;

public class ContinentsFloorsRegionsClient
    : BaseBulkAllClient<ContinentFloorRegion, int>,
        IContinentsFloorsRegionsClient
{
    private readonly int _continentId;
    private readonly int _floorId;

    internal ContinentsFloorsRegionsClient(HttpClient httpClient, int continentId, int floorId)
        : base(httpClient)
    {
        _continentId = continentId;
        _floorId = floorId;
    }

    protected override string UriPath => $"/v2/continents/{_continentId}/floors/{_floorId}/regions";

    public IContinentsFloorsRegionsIdClient this[int regionId] =>
        new ContinentsFloorsRegionsIdClient(HttpClient, _continentId, _floorId, regionId);
}
