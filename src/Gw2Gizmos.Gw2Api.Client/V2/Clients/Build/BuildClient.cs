namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Build;

public sealed class BuildClient : BaseBlobClient<Contract.V2.Build.Build>, IBuildClient
{
    internal BuildClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/build";
}
