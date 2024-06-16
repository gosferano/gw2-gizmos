namespace Gw2Gizmos.Gw2Api.Client.Clients;

public abstract class BasePaginatedBlobClient<TResponse>
    : BaseClient,
        IBlobClient<TResponse[]>,
        IPaginatedClient<TResponse>
{
    internal BasePaginatedBlobClient(HttpClient httpClient)
        : base(httpClient) { }

    public Task<TResponse[]> GetBlob(CancellationToken cancellationToken = default)
    {
        return GetBlob<TResponse[]>(cancellationToken);
    }

    public Task<TResponse[]> GetPage(int page, CancellationToken cancellationToken = default)
    {
        return GetPage<TResponse>(page, cancellationToken);
    }

    public Task<TResponse[]> GetPage(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return GetPage<TResponse>(page, pageSize, cancellationToken);
    }
}
