namespace Gw2Gizmos.Gw2Api.Client.V2.Clients;

public interface IBlobClient<TResponse>
{
    Task<TResponse> GetBlob(CancellationToken cancellationToken = default);
}
