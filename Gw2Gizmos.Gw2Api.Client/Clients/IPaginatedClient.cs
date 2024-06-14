namespace Gw2Gizmos.Gw2Api.Client.Clients;

public interface IPaginatedClient<TResponse>
{
    Task<TResponse[]> GetPage(int page, CancellationToken cancellationToken = default);
    Task<TResponse[]> GetPage(int page, int pageSize, CancellationToken cancellationToken = default);
}
