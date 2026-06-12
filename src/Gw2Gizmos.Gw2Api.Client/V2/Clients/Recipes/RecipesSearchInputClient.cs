namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Recipes;

public sealed class RecipesSearchInputClient : BaseClient, IRecipesSearchInputClient
{
    private readonly int _inputItemId;

    internal RecipesSearchInputClient(HttpClient httpClient, int inputItemId)
        : base(httpClient)
    {
        _inputItemId = inputItemId;
    }

    protected override string UriPath => "/v2/recipes/search";

    public Task<Result<int[], Error>> GetBlob(CancellationToken cancellationToken = default)
    {
        return Get<int[]>($"{UriPath}?input={_inputItemId}", SchemaVersion, cancellationToken);
    }
}
