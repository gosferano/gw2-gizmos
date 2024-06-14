namespace Gw2Gizmos.Gw2Api.Client.Clients;

public interface IBlobClient<TResponse>
{
    Task<TResponse> GetBlob(CancellationToken cancellationToken = default);
}
