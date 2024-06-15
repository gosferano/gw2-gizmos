namespace Gw2Gizmos.Gw2Api.Client.Clients;

public abstract class BaseClient
{
    private readonly string _idsParameterName;
    private const string SchemaVersion = "2022-03-23T19:00:00.000Z";

    internal BaseClient(IGw2ApiClient apiClient, string idsParameterName = "ids")
    {
        _idsParameterName = idsParameterName;
        ApiClient = apiClient;
    }

    protected abstract string UriPath { get; }

    internal IGw2ApiClient ApiClient { get; }

    protected Task<TResponse> GetBlob<TResponse>(CancellationToken cancellationToken)
    {
        return ApiClient.Get<TResponse>(UriPath, SchemaVersion, cancellationToken);
    }

    protected Task<TResponse> GetById<TResponse, TId>(TId id, CancellationToken cancellationToken)
    {
        return ApiClient.Get<TResponse>($"{UriPath}/{id}", SchemaVersion, cancellationToken);
    }

    protected Task<TId[]> GetIds<TId>(CancellationToken cancellationToken)
    {
        return ApiClient.Get<TId[]>(UriPath, SchemaVersion, cancellationToken);
    }

    protected Task<TResponse[]> GetByIds<TResponse, TId>(IEnumerable<TId> ids, CancellationToken cancellationToken)
    {
        return ApiClient.Get<TResponse[]>(
            $"{UriPath}?{_idsParameterName}=" + string.Join(",", ids),
            SchemaVersion,
            cancellationToken
        );
    }

    protected Task<TResponse[]> GetPage<TResponse>(int page, CancellationToken cancellationToken)
    {
        return ApiClient.Get<TResponse[]>($"{UriPath}?page={page}", SchemaVersion, cancellationToken);
    }

    protected Task<TResponse[]> GetPage<TResponse>(int page, int pageSize, CancellationToken cancellationToken)
    {
        return ApiClient.Get<TResponse[]>(
            $"{UriPath}?page={page}&page_size={pageSize}",
            SchemaVersion,
            cancellationToken
        );
    }

    protected Task<TResponse[]> GetAll<TResponse>(CancellationToken cancellationToken)
    {
        return ApiClient.Get<TResponse[]>($"{UriPath}?{_idsParameterName}=all", SchemaVersion, cancellationToken);
    }
}
