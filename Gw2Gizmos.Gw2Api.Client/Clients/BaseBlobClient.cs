namespace Gw2Gizmos.Gw2Api.Client.Clients;

public abstract class BaseBlobClient<TResponse> : BaseClient, IBlobClient<TResponse>
{
    internal BaseBlobClient(HttpClient httpClient)
        : base(httpClient) { }

    public Task<TResponse> GetBlob(CancellationToken cancellationToken = default)
    {
        return GetBlob<TResponse>(cancellationToken);
    }
}
