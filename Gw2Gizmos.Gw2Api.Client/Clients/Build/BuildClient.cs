namespace Gw2Gizmos.Gw2Api.Client.Clients.Build;

public class BuildClient : BaseBlobClient<Contract.Build.Build>, IBuildClient
{
    internal BuildClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/build";
}
