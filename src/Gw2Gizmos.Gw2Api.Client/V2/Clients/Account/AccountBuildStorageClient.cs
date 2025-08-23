using Gw2Gizmos.Gw2Api.Contract.V2.BuildStorage;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Account;

public class AccountBuildStorageClient : BaseBulkAllClient<BuildStorageBuild, int>, IAccountBuildStorageClient
{
    internal AccountBuildStorageClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/account/buildstorage";
}
