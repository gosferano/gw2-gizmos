namespace Gw2Gizmos.Gw2Api.Client.V2.Clients;

public abstract class BasePaginatedBlobClient<TResponse>
    : BaseClient,
        IBlobClient<TResponse[]>,
        IPaginatedClient<TResponse>
{
    internal BasePaginatedBlobClient(HttpClient httpClient)
        : base(httpClient) { }

    public Task<Result<TResponse[], Error>> GetBlob(CancellationToken cancellationToken = default)
    {
        return GetBlob<TResponse[]>(cancellationToken);
    }

    public Task<Result<TResponse[], Error>> GetPage(int page, CancellationToken cancellationToken = default)
    {
        return GetPage<TResponse>(page, cancellationToken);
    }

    public Task<Result<TResponse[], Error>> GetPage(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default
    )
    {
        return GetPage<TResponse>(page, pageSize, cancellationToken);
    }
}
