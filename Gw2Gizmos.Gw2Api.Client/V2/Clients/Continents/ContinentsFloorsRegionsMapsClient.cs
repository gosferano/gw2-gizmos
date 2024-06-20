using Gw2Gizmos.Gw2Api.Contract.Continents;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Continents;

public class ContinentsFloorsRegionsMapsClient
    : BaseBulkAllClient<ContinentFloorRegionMap, int>,
        IContinentsFloorsRegionsMapsClient
{
    private readonly int _continentId;
    private readonly int _floorId;
    private readonly int _regionId;

    internal ContinentsFloorsRegionsMapsClient(HttpClient httpClient, int continentId, int floorId, int regionId)
        : base(httpClient)
    {
        _continentId = continentId;
        _floorId = floorId;
        _regionId = regionId;
    }

    protected override string UriPath => $"/v2/continents/{_continentId}/floors/{_floorId}/regions/{_regionId}/maps";

    public IContinentsFloorsRegionsMapsIdClient this[int mapId] =>
        new ContinentsFloorsRegionsMapsIdClient(HttpClient, _continentId, _floorId, _regionId, mapId);
}
