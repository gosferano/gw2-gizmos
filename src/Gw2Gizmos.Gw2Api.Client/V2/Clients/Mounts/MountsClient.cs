using Gw2Gizmos.Gw2Api.Contract.V2.Mounts;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Mounts;

public sealed class MountsClient : BaseBlobClient<string[]>, IMountsClient
{
    public MountsClient(HttpClient httpClient)
        : base(httpClient)
    {
        Skins = new MountsSkinsClient(httpClient);
        Types = new MountsTypesClient(httpClient);
    }

    protected override string UriPath => "/v2/mounts";
    public IMountsSkinsClient Skins { get; }
    public IMountsTypesClient Types { get; }
}

public sealed class MountsTypesClient : BaseBulkAllClient<MountType, string>, IMountsTypesClient
{
    internal MountsTypesClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/mounts/types";
}

public sealed class MountsSkinsClient : BaseBulkAllClient<MountSkin, int>, IMountsSkinsClient
{
    internal MountsSkinsClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/mounts/skins";
}
