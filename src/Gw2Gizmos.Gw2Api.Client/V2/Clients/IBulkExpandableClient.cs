namespace Gw2Gizmos.Gw2Api.Client.V2.Clients;

public interface IBulkExpandableClient<TResponse, TId>
{
    Task<TResponse> GetById(TId id, CancellationToken cancellationToken = default);
    Task<TId[]> GetIds(CancellationToken cancellationToken = default);
    Task<TResponse[]> GetByIds(IEnumerable<TId> ids, CancellationToken cancellationToken = default);
}
