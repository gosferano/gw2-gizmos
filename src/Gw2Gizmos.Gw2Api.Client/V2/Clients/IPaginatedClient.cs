namespace Gw2Gizmos.Gw2Api.Client.V2.Clients;

public interface IPaginatedClient<TResponse>
{
    Task<Result<TResponse[], Error>> GetPage(int page, CancellationToken cancellationToken = default);
    Task<Result<TResponse[], Error>> GetPage(int page, int pageSize, CancellationToken cancellationToken = default);
}
