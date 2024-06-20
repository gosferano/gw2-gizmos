using Gw2Gizmos.Gw2Api.Contract.Mounts;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Mounts;

public interface IMountsClient : IBlobClient<string[]>
{
    public IMountsSkinsClient Skins { get; }
    public IMountsTypesClient Types { get; }
}

public interface IMountsTypesClient
    : IAllExpandableClient<MountType>,
        IBulkExpandableClient<MountType, string>,
        IPaginatedClient<MountType>;

public interface IMountsSkinsClient
    : IAllExpandableClient<MountSkin>,
        IBulkExpandableClient<MountSkin, int>,
        IPaginatedClient<MountSkin>;
