namespace Gw2Gizmos.Gw2Api.Client.Clients.Continents;

public interface IContinentsFloorsRegionsMapsIdClient
{
    public IContinentsFloorsRegionsMapsPoisClient Pois { get; }
    public IContinentsFloorsRegionsMapsSectorsClient Sectors { get; }
    public IContinentsFloorsRegionsMapsTasksClient Tasks { get; }
}
