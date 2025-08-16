namespace Gw2Gizmos.Gw2Api.Client.V2.Clients;

public interface IBulkExpandableClient<TResponse, TId>
{
    Task<Result<TResponse, Error>> GetById(TId id, CancellationToken cancellationToken = default);
    Task<Result<TId[], Error>> GetIds(CancellationToken cancellationToken = default);
    Task<Result<TResponse[], Error>> GetByIds(IEnumerable<TId> ids, CancellationToken cancellationToken = default);
}
