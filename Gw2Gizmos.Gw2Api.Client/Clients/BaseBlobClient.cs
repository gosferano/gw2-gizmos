namespace Gw2Gizmos.Gw2Api.Client.Clients;

public abstract class BaseBlobClient<TResponse> : BaseClient, IBlobClient<TResponse>
{
    internal BaseBlobClient(IGw2ApiClient apiClient)
        : base(apiClient) { }

    public Task<TResponse> GetBlob(CancellationToken cancellationToken = default)
    {
        return GetBlob<TResponse>(cancellationToken);
    }
}
