namespace Gw2Gizmos.Gw2Api.Client.Clients.Continents;

public class ContinentsIdClient : BaseClient, IContinentsIdClient
{
    private readonly int _continentId;

    internal ContinentsIdClient(HttpClient httpClient, int continentId)
        : base(httpClient)
    {
        _continentId = continentId;
        Floors = new ContinentsFloorsClient(httpClient, continentId);
    }

    protected override string UriPath => $"/v2/continents/{_continentId}";
    public IContinentsFloorsClient Floors { get; }
}
