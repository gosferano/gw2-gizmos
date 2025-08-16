namespace Gw2Gizmos.Gw2Api.Client.V2.Clients;

public abstract class BaseBlobClient<TResponse> : BaseClient, IBlobClient<TResponse>
{
    internal BaseBlobClient(HttpClient httpClient)
        : base(httpClient) { }

    public Task<Result<TResponse, Error>> GetBlob(CancellationToken cancellationToken = default)
    {
        return GetBlob<TResponse>(cancellationToken);
    }
}
