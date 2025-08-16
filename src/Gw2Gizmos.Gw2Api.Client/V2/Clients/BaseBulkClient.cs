namespace Gw2Gizmos.Gw2Api.Client.V2.Clients;

public abstract class BaseBulkClient<TResponse, TId>
    : BaseClient,
        IBulkExpandableClient<TResponse, TId>,
        IPaginatedClient<TResponse>
{
    internal BaseBulkClient(HttpClient httpClient)
        : base(httpClient) { }

    public Task<Result<TResponse, Error>> GetById(TId id, CancellationToken cancellationToken = default)
    {
        return GetById<TResponse, TId>(id, cancellationToken);
    }

    public Task<Result<TId[], Error>> GetIds(CancellationToken cancellationToken = default)
    {
        return GetIds<TId>(cancellationToken);
    }

    public Task<Result<TResponse[], Error>> GetByIds(
        IEnumerable<TId> ids,
        CancellationToken cancellationToken = default
    )
    {
        return GetByIds<TResponse, TId>(ids, cancellationToken);
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
