namespace Gw2Gizmos.Gw2Api.Client.V2.Clients;

public abstract class BaseBulkClient<TResponse, TId>
    : BaseClient,
        IBulkExpandableClient<TResponse, TId>,
        IPaginatedClient<TResponse>
{
    internal BaseBulkClient(HttpClient httpClient)
        : base(httpClient) { }

    public Task<TResponse> GetById(TId id, CancellationToken cancellationToken = default)
    {
        return GetById<TResponse, TId>(id, cancellationToken);
    }

    public Task<TId[]> GetIds(CancellationToken cancellationToken = default)
    {
        return GetIds<TId>(cancellationToken);
    }

    public Task<TResponse[]> GetByIds(IEnumerable<TId> ids, CancellationToken cancellationToken = default)
    {
        return GetByIds<TResponse, TId>(ids, cancellationToken);
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
