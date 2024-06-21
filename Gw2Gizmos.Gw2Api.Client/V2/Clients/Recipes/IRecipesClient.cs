using Gw2Gizmos.Gw2Api.Contract.Recipes;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Recipes;

public interface IRecipesClient : IBulkExpandableClient<Recipe, int>, IPaginatedClient<Recipe>;
