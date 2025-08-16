namespace Gw2Gizmos.Gw2Api.Client.V2.Clients;

public interface IAllExpandableClient<TResponse>
{
    Task<TResponse[]> GetAll(CancellationToken cancellationToken = default);
}
