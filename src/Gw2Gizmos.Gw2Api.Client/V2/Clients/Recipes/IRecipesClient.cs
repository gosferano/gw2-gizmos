using Gw2Gizmos.Gw2Api.Contract.V2.Recipes;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Recipes;

public interface IRecipesClient : IBulkExpandableClient<Recipe, int>, IPaginatedClient<Recipe>
{
    public IRecipesSearchClient Search { get; }
}
