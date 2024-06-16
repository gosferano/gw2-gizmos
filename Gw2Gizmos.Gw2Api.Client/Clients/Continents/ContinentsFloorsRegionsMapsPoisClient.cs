using Gw2Gizmos.Gw2Api.Contract.Continents;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Continents;

public class ContinentsFloorsRegionsMapsPoisClient
    : BaseBulkAllClient<ContinentFloorRegionMapPoi, int>,
        IContinentsFloorsRegionsMapsPoisClient
{
    private readonly int _continentId;
    private readonly int _floorId;
    private readonly int _regionId;
    private readonly int _mapId;

    internal ContinentsFloorsRegionsMapsPoisClient(
        HttpClient httpClient,
        int continentId,
        int floorId,
        int regionId,
        int mapId
    )
        : base(httpClient)
    {
        _continentId = continentId;
        _floorId = floorId;
        _regionId = regionId;
        _mapId = mapId;
    }

    protected override string UriPath =>
        $"/v2/continents/{_continentId}/floors/{_floorId}/regions/{_regionId}/maps/{_mapId}/pois";
}
