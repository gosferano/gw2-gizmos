namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Continents;

public sealed class ContinentsFloorsRegionsIdClient : BaseClient, IContinentsFloorsRegionsIdClient
{
    private readonly int _continentId;
    private readonly int _floorId;
    private readonly int _regionId;

    public ContinentsFloorsRegionsIdClient(HttpClient httpClient, int continentId, int floorId, int regionId)
        : base(httpClient)
    {
        _continentId = continentId;
        _floorId = floorId;
        _regionId = regionId;
        Maps = new ContinentsFloorsRegionsMapsClient(httpClient, continentId, floorId, regionId);
    }

    protected override string UriPath => $"/v2/continents/{_continentId}/floors/{_floorId}/regions/{_regionId}";

    public IContinentsFloorsRegionsMapsClient Maps { get; }
}
