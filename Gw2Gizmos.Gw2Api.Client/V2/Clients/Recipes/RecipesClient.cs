using Gw2Gizmos.Gw2Api.Contract.Recipes;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Recipes;

public class RecipesClient : BaseBulkClient<Recipe, int>, IRecipesClient
{
    internal RecipesClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/recipes";
}
