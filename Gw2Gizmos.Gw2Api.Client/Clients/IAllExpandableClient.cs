namespace Gw2Gizmos.Gw2Api.Client.Clients;

public interface IAllExpandableClient<TResponse>
{
    Task<TResponse[]> GetAll(CancellationToken cancellationToken);
}
