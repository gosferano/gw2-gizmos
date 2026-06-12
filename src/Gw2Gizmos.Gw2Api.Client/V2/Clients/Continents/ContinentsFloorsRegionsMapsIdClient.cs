namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Continents;

public sealed class ContinentsFloorsRegionsMapsIdClient : BaseClient, IContinentsFloorsRegionsMapsIdClient
{
    private readonly int _continentId;
    private readonly int _floorId;
    private readonly int _regionId;
    private readonly int _mapId;

    internal ContinentsFloorsRegionsMapsIdClient(
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
        Pois = new ContinentsFloorsRegionsMapsPoisClient(httpClient, continentId, floorId, regionId, mapId);
        Sectors = new ContinentsFloorsRegionsMapsSectorsClient(httpClient, continentId, floorId, regionId, mapId);
        Tasks = new ContinentsFloorsRegionsMapsTasksClient(httpClient, continentId, floorId, regionId, mapId);
    }

    protected override string UriPath =>
        $"/v2/continents/{_continentId}/floors/{_floorId}/regions/{_regionId}/maps/{_mapId}";

    public IContinentsFloorsRegionsMapsPoisClient Pois { get; }
    public IContinentsFloorsRegionsMapsSectorsClient Sectors { get; }
    public IContinentsFloorsRegionsMapsTasksClient Tasks { get; }
}
