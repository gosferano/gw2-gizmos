using File = Gw2Gizmos.Gw2Api.Contract.V2.Files.File;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Files;

public sealed class FilesClient : BaseBulkAllClient<File, string>, IFilesClient
{
    internal FilesClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/files";
}
