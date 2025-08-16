namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Continents;

public class ContinentsFloorsIdClient : BaseClient, IContinentsFloorsIdClient
{
    private readonly int _continentId;
    private readonly int _floorId;

    internal ContinentsFloorsIdClient(HttpClient httpClient, int continentId, int floorId)
        : base(httpClient)
    {
        _continentId = continentId;
        _floorId = floorId;
        Regions = new ContinentsFloorsRegionsClient(httpClient, continentId, floorId);
    }

    protected override string UriPath => $"/v2/continents/{_continentId}/floors/{_floorId}";

    public IContinentsFloorsRegionsClient Regions { get; }
}
