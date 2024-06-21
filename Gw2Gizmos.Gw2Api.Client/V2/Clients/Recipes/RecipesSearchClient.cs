namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Recipes;

public class RecipesSearchClient : BaseClient, IRecipesSearchClient
{
    internal RecipesSearchClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/recipes/search";

    public IRecipesSearchInputClient WithInputItemId(int inputItemId)
    {
        return new RecipesSearchInputClient(HttpClient, inputItemId);
    }

    public IRecipesSearchOutputClient WithOutputItemId(int outputItemId)
    {
        return new RecipesSearchOutputClient(HttpClient, outputItemId);
    }
}
