namespace Gw2Gizmos.Gw2Api.Client.V2.Clients;

public interface IAllExpandableClient<TResponse>
{
    Task<Result<TResponse[], Error>> GetAll(CancellationToken cancellationToken = default);
}
