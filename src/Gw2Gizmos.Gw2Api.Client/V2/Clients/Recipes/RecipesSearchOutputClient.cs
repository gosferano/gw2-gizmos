namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Recipes;

public class RecipesSearchOutputClient : BaseClient, IRecipesSearchOutputClient
{
    private readonly int _outputItemId;

    internal RecipesSearchOutputClient(HttpClient httpClient, int outputItemId)
        : base(httpClient)
    {
        _outputItemId = outputItemId;
    }

    protected override string UriPath => "/v2/recipes/search";

    public Task<Result<int[], Error>> GetBlob(CancellationToken cancellationToken = default)
    {
        return Get<int[]>($"{UriPath}?output={_outputItemId}", SchemaVersion, cancellationToken);
    }
}
